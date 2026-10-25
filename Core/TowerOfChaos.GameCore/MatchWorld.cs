using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerOfChaos.GameCore
{
    /// <summary>
    /// Whole-tower world for one match (SPEC 14.2 step 4): all rooms + portal plan from a single matchSeed.
    /// Enforces the per-floor rules a single room cannot know about:
    /// mutators in at most 25% of a floor's rooms (never room 1, never floor 1), portals in rooms 2..9 only.
    /// Every room is re-validated; on any violation the world is reseeded (up to MaxAttempts).
    /// </summary>
    public sealed class MatchWorld
    {
        public const int MaxAttempts = 5;
        public TowerConfig Config;
        public long Seed;
        public int Attempts;
        public RoomLayout[,] Rooms;           // [floor-1, room-1]
        public List<PortalPlan> Portals;

        public RoomLayout Room(int floor, int room) { return Rooms[floor - 1, room - 1]; }

        public static MatchWorld Build(TowerConfig cfg, Catalog cat, long matchSeed, int activePlayers = 5)
        {
            var gen = new SolvableRoomGenerator(cfg, cat);
            var portals = PortalPlanner.Plan(cfg, matchSeed);
            int mutatorCap = (int)Math.Floor(TowerConfig.RoomsPerFloor * 0.25);   // 2 rooms per floor

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                long seed = unchecked(matchSeed + attempt * 1000003L);
                var rooms = new RoomLayout[cfg.Height, TowerConfig.RoomsPerFloor];
                bool ok = true;
                for (int f = 1; f <= cfg.Height && ok; f++)
                {
                    var rng = DeterministicRng.For(seed, f, 0, 7002);
                    var cand = new List<int>();
                    for (int r = 2; r <= TowerConfig.RoomsPerFloor; r++) cand.Add(r);
                    for (int i = cand.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = cand[i]; cand[i] = cand[j]; cand[j] = t; }
                    var mutRooms = new HashSet<int>(f >= 2 ? cand.Take(mutatorCap) : Enumerable.Empty<int>());

                    for (int r = 1; r <= TowerConfig.RoomsPerFloor && ok; r++)
                    {
                        var q = new RoomRequest
                        {
                            Floor = f, Room = r, MatchSeed = seed, ActivePlayers = activePlayers,
                            HasPortal = portals.Any(p => p.SourceFloor == f && p.SourceRoom == r),
                            Mutator = mutRooms.Contains(r) ? MutatorMode.Force : MutatorMode.None
                        };
                        var L = gen.Generate(q);
                        if (RoomValidator.Validate(L, cat, cfg).Count > 0) ok = false;
                        rooms[f - 1, r - 1] = L;
                    }
                }
                if (ok) return new MatchWorld { Config = cfg, Seed = seed, Attempts = attempt + 1, Rooms = rooms, Portals = portals };
            }
            throw new InvalidOperationException("Could not build a valid world in " + MaxAttempts + " attempts");
        }
    }
}
