using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerOfChaos.GameCore
{
    /// <summary>Budget, scaling and pacing formulas: SPEC 15 and 29.3.</summary>
    public static class TowerRules
    {
        public const double MinSafeWindowSeconds = 1.2;   // SPEC 6.1 rule 3
        public const double FogDamagePerSecond = 25.0;
        public const double FogStepSeconds = 25.0;
        public const double TrapZoneMaxOccupancy = 0.45;  // SPEC 15.2

        /// <summary>B(f) = 8 + 1.5 (f-1)</summary>
        public static double FloorBase(int floor) { return 8.0 + 1.5 * (floor - 1); }

        public static double RoomMultiplier(int room)
        {
            if (room == 1) return 0.5;
            if (room == TowerConfig.RoomsPerFloor) return 1.5;
            return 1.0;
        }

        /// <summary>TBP(f, r) = round(B(f) * m(r)) * playerLoadFactor</summary>
        public static int TrapBudget(int floor, int room, int activePlayers)
        {
            double v = FloorBase(floor) * RoomMultiplier(room) * PlayerLoadFactor(activePlayers);
            return (int)Math.Round(v, MidpointRounding.AwayFromZero);
        }

        public static double PlayerLoadFactor(int k)
        {
            if (k >= 5) return 1.00;
            if (k == 4) return 0.90;
            if (k == 3) return 0.80;
            return 0.70;
        }

        public static double MonsterHpFactor(int k)
        {
            if (k >= 5) return 1.00;
            if (k == 4) return 0.95;
            if (k == 3) return 0.90;
            return 0.85;
        }

        /// <summary>HP = base * (1 + 0.25 (n-1)), capped at x2 (n = players inside the room).</summary>
        public static double BossHp(double baseHp, int playersInRoom)
        {
            double f = 1.0 + 0.25 * (Math.Max(1, playersInRoom) - 1);
            return baseHp * Math.Min(2.0, f);
        }

        /// <summary>Interactive multiplayer traps: x2 in crowded rooms (n>=3), x0.5 when alone.</summary>
        public static double InteractiveWeight(int playersInRoom)
        {
            if (playersInRoom >= 3) return 2.0;
            if (playersInRoom <= 1) return 0.5;
            return 1.0;
        }

        public static int MaxTierCPerRoom(int floor) { return floor <= 3 ? 1 : 2; }

        /// <summary>
        /// Timed gates (lanes / timed tiles) the golden path may cross in one room. v6.2 rule found by the physical bot test:
        /// without a cap and a safe tile between gates, a flawless run could not keep the SPEC 15.1 pace (and the fog caught it).
        /// </summary>
        public static int MaxTimedGatesPerRoom(int floor) { return floor <= 2 ? 1 : 2; }

        /// <summary>
        /// SPEC 6.1 rule 3: the golden path must be passable with ZERO damage, so only timed traps with a >= 1.2 s window
        /// or harmless (tier S: no damage, no crowd control) traps may sit on it. Everything else goes off-path.
        /// </summary>
        public static bool IsAllowedOnGoldenPath(TrapDef d)
        {
            if (d.Policy == PathPolicy.TimedBlocks) return d.SafeWindow >= MinSafeWindowSeconds;
            if (d.Policy == PathPolicy.BlocksWithBypass) return false;
            return d.Tier == CostTier.S;
        }

        // ---- pacing (SPEC 29.3): E(H) = 45 + 67.5 H seconds (H=3: 4.1 min, H=6: 7.5 min, H=10: 12 min)
        public static double ExpectedFirstFinishSeconds(int h) { return 45.0 + 67.5 * h; }
        public static double FogStartSeconds(int h) { return 0.75 * ExpectedFirstFinishSeconds(h); }
        public static double FogReachesFloorAt(int h, int floor) { return FogStartSeconds(h) + FogStepSeconds * (floor - 1); }
        public static double HardCapSeconds(int h) { return ExpectedFirstFinishSeconds(h) + 120.0; }

        /// <summary>Fog covers a floor once the clock passed its schedule.</summary>
        public static bool IsFloorFogged(int h, int floor, double elapsedSeconds) { return elapsedSeconds >= FogReachesFloorAt(h, floor); }
    }

    // ------------------------------------------------------------------------------------------ ranking
    public enum PlayerStatus { Alive, Eliminated, Forfeit }

    public sealed class PlayerResult
    {
        public string Id;
        public PlayerStatus Status;
        public bool CrownHolder;
        public int MaxProgress;          // best (floor-1)*10+room ever reached
        public double ReachedAtSec;      // time MaxProgress was first reached (smaller = earlier)
        public double EliminatedAtSec;   // only for Eliminated
    }

    public static class Ranking
    {
        /// <summary>SPEC 17.5: crown holder, alive (progress desc, earlier first), eliminated (progress desc, later elimination first), forfeit.</summary>
        public static List<PlayerResult> Rank(IEnumerable<PlayerResult> players)
        {
            Func<PlayerResult, int> group = p => p.CrownHolder ? 0 : p.Status == PlayerStatus.Alive ? 1 : p.Status == PlayerStatus.Eliminated ? 2 : 3;
            return players
                .OrderBy(group)
                .ThenByDescending(p => p.MaxProgress)
                .ThenBy(p => p.Status == PlayerStatus.Eliminated ? -p.EliminatedAtSec : p.ReachedAtSec)
                .ThenBy(p => p.Id, StringComparer.Ordinal)
                .ToList();
        }

        public static int ActivePlayers(IEnumerable<PlayerResult> players) { return players.Count(p => p.Status == PlayerStatus.Alive); }

        public enum EndReason { None, CrownCaptured, LastActivePlayer, TimeCap }

        /// <summary>SPEC 17.3.</summary>
        public static EndReason EvaluateEnd(IReadOnlyList<PlayerResult> players, double elapsedSeconds, int h)
        {
            if (players.Any(p => p.CrownHolder)) return EndReason.CrownCaptured;
            if (ActivePlayers(players) <= 1) return EndReason.LastActivePlayer;
            if (elapsedSeconds >= TowerRules.HardCapSeconds(h)) return EndReason.TimeCap;
            return EndReason.None;
        }
    }

    // ------------------------------------------------------------------------------------------ dice & death
    public struct RespawnInfo { public int Floor, Room; public double DelaySeconds, SpawnShieldSeconds; public bool DropsLoot; }

    public static class MatchFlow
    {
        /// <summary>Start dice: one roll per player, 1..6, ties allowed (SPEC 14.2).</summary>
        public static int RollStartRoom(DeterministicRng rng) { return rng.Next(1, 7); }

        /// <summary>SPEC 11.3: death anywhere -> room 1 of the SAME floor (never falls a floor); room 1 death adds a 3..5 s pause.</summary>
        public static RespawnInfo Respawn(int floor, int room, DeterministicRng rng)
        {
            double delay = room == 1 ? 3.0 + 2.0 * rng.NextDouble() : 2.0;
            return new RespawnInfo { Floor = floor, Room = 1, DelaySeconds = delay, SpawnShieldSeconds = 3.0, DropsLoot = true };
        }
    }

    /// <summary>Max 3 scans per match from ALL sources (gems, R10, VIP) - SPEC 21.2.</summary>
    public sealed class ScanBudget
    {
        public const int MaxPerMatch = 3;
        private readonly HashSet<string> _requests = new HashSet<string>();
        public int Used { get { return _requests.Count; } }

        /// <summary>Idempotent: the same requestId never consumes twice (SPEC 22 #27).</summary>
        public bool TryUse(string requestId)
        {
            if (_requests.Contains(requestId)) return true;
            if (_requests.Count >= MaxPerMatch) return false;
            _requests.Add(requestId);
            return true;
        }

        /// <summary>Gems are charged only on delivery; undelivered requests are refunded.</summary>
        public void Refund(string requestId) { _requests.Remove(requestId); }
    }

    // ------------------------------------------------------------------------------------------ portals
    public sealed class PortalPlan
    {
        public int SourceFloor, SourceRoom, DestFloor, DestRoom;
        public bool Ascending, Mirage;
    }

    public static class PortalPlanner
    {
        /// <summary>SPEC 9 + 17.6. Source rooms 2..9 (never 1 or 10); ascending cap = floor H room 5; top-floor portals are 80% descending, rest mirage.</summary>
        public static List<PortalPlan> Plan(TowerConfig cfg, long matchSeed)
        {
            var plans = new List<PortalPlan>();
            int h = cfg.Height;
            for (int f = 1; f <= h; f++)
            {
                var rng = DeterministicRng.For(matchSeed, f, 0, 7001);
                int count = (f == 4 || f == 5 || f == 8) ? 3 : 2;
                var rooms = new List<int>();
                for (int r = 2; r <= 9; r++) rooms.Add(r);
                for (int i = rooms.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = rooms[i]; rooms[i] = rooms[j]; rooms[j] = t; }
                for (int i = 0; i < count; i++)
                {
                    var p = new PortalPlan { SourceFloor = f, SourceRoom = rooms[i] };
                    double pDown = f == 1 ? 0.0 : f == h ? 0.8 : 0.5;
                    bool top = f == h;
                    if (h == 1) { p.Mirage = true; }
                    else if (f == 1) { p.Ascending = true; }
                    else if (top) { if (rng.NextDouble() < pDown) { p.Ascending = false; } else { p.Mirage = true; } }
                    else { p.Ascending = rng.NextDouble() >= pDown; }

                    if (p.Mirage) { p.DestFloor = f; p.DestRoom = p.SourceRoom; }
                    else if (p.Ascending)
                    {
                        p.DestFloor = Math.Min(f + 1 + rng.Next(2), h);
                        p.DestRoom = p.DestFloor == h ? rng.Next(2, 6) : rng.Next(2, 10);
                    }
                    else
                    {
                        p.DestFloor = Math.Max(1, f - 1 - rng.Next(2));
                        p.DestRoom = rng.Next(2, 10);
                    }
                    plans.Add(p);
                }
            }
            return plans;
        }
    }

    // ------------------------------------------------------------------------------------------ progression
    public sealed class LevelReward
    {
        public int Level, Gold, ChatLine, VoiceLine, Gems, SkinHero = -1, HeroUnlock = -1;
        public string SkinRarity, Chest = "None";
        public bool AvatarFrame;
    }

    public static class Progression
    {
        public static readonly string[] Heroes = { "Kaveh", "Nyx", "Kimia", "Rostam", "Mitra" };

        /// <summary>XP to go from level L to L+1 (SPEC 12.1).</summary>
        public static int XpRequired(int level) { return 60 + 30 * level; }

        public static long TotalXpToReach(int level)
        {
            long sum = 0;
            for (int l = 1; l < level; l++) sum += XpRequired(l);
            return sum;
        }

        public static int LevelCap(bool releaseV1) { return releaseV1 ? 40 : 60; }

        public static int LevelForXp(long xp, int cap)
        {
            int lvl = 1;
            while (lvl < cap && xp >= TotalXpToReach(lvl + 1)) lvl++;
            return lvl;
        }

        /// <summary>SPEC 19.2 generation rules. In v1.0 the level-20 Epic goes to Kimia (Rostam/Mitra Epic skins ship in Update 1).</summary>
        public static LevelReward RewardFor(int L, bool releaseV1)
        {
            var r = new LevelReward { Level = L, Gold = 50 + 10 * L };
            if (L == 6) r.HeroUnlock = 3;
            if (L == 12) r.HeroUnlock = 4;
            if (L % 2 == 0) r.ChatLine = L / 2;
            if (L % 3 == 0) r.VoiceLine = L / 3;
            if (L % 5 == 0)
            {
                r.SkinHero = (L / 5 - 1) % 5;
                r.SkinRarity = (L == 40 || L == 60) ? "Mythic" : (L == 10 || L == 20 || L == 30 || L == 50) ? "Epic" : "Rare";
                if (releaseV1 && L == 20) r.SkinHero = 2;
            }
            if (L % 7 == 0) r.AvatarFrame = true;
            if (L % 10 == 0) r.Gems = 20;
            if (L == 1) r.Chest = "Wooden";
            else if (L == 60) r.Chest = "Arcana";
            else if (L == 50) r.Chest = "Mythic";
            else if (L == 25 || L == 45) r.Chest = "Golden";
            else if (L % 10 == 9) r.Chest = "Silver";
            else if (L % 4 == 1) r.Chest = "Wooden";
            return r;
        }
    }
}
