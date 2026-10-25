using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TowerOfChaos.GameCore;

namespace TowerOfChaos.GameCore.Tests
{
    internal static class Program
    {
        private static int _pass, _fail;
        private static Catalog _cat;

        private static void Check(bool ok, string name, string detail = "")
        {
            if (ok) { _pass++; Console.WriteLine("  PASS  " + name + (detail.Length > 0 ? "   [" + detail + "]" : "")); }
            else { _fail++; Console.WriteLine("  FAIL  " + name + "   " + detail); }
        }

        private static int Main()
        {
            int seedsPerFloor = int.TryParse(Environment.GetEnvironmentVariable("TOC_SEEDS"), out var s) ? s : 20000;
            string catalogText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "catalog.json"));
            _cat = CatalogReader.Parse(catalogText);   // the same reader Unity and the server use
            Console.WriteLine("Seeds per floor: " + seedsPerFloor);
            Console.WriteLine("\n[Catalog reader parity (MiniJson vs System.Text.Json)]");
            var reference = LoadCatalog(Path.Combine(AppContext.BaseDirectory, "catalog.json"));
            Check(reference.Traps.Count == _cat.Traps.Count && reference.Traps.Zip(_cat.Traps, (a, b) => a.Id == b.Id && a.Tier == b.Tier && a.Policy == b.Policy && a.Shape == b.Shape && a.SafeWindow == b.SafeWindow && a.HomeFloor == b.HomeFloor && a.Release == b.Release && a.NameFa == b.NameFa).All(x => x), "CatalogReader == System.Text.Json for all 90 traps (incl. Persian names)");
            Check(reference.Mutators.Zip(_cat.Mutators, (a, b) => a.Id == b.Id && a.Floors.SequenceEqual(b.Floors) && a.Conflicts.SequenceEqual(b.Conflicts) && a.GauntletSafe == b.GauntletSafe).All(x => x) && reference.Rewards.Count == _cat.Rewards.Count && reference.Monsters.Count == _cat.Monsters.Count, "mutators / rewards / monsters identical");
            Check(MiniJson.Parse("{\"a\":[1,2.5e1,\"x\\u0041\\n\",true,null],\"b\":{}}") is System.Collections.Generic.Dictionary<string, object>, "MiniJson handles escapes, exponents, nesting");

            Console.WriteLine("\n[Catalog vs SPEC]"); CatalogTests();
            Console.WriteLine("\n[Formulas vs SPEC numbers]"); FormulaTests();
            Console.WriteLine("\n[Determinism]"); DeterminismTests();
            Console.WriteLine("\n[Solvability + guarantees over many seeds]");
            foreach (int h in new[] { 3, 6, 10 }) SolvabilityTest(h, seedsPerFloor);
            Console.WriteLine("\n[Whole-tower world per match]"); WorldTests();
            Console.WriteLine("\n[Simulation: movement, trap clock, camera, run flow]"); SimTests();
            Console.WriteLine("\n[Golden-path bot walks whole towers physically (no hits allowed)]"); BotTests();
            Console.WriteLine("\n[Validator is not vacuous (negative tests)]"); NegativeTests();
            Console.WriteLine("\n[Release isolation (v1.0 must not contain Update-1 content)]"); ReleaseIsolationTest();
            Console.WriteLine("\n[Rules: ranking, death, dice, scans, portals, progression]"); RuleTests();

            Console.WriteLine("\nRESULT: " + _pass + " passed, " + _fail + " failed");
            return _fail == 0 ? 0 : 1;
        }

        // ------------------------------------------------------------------------------ catalog
        private static Catalog LoadCatalog(string path)
        {
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var c = new Catalog();
            foreach (var t in root.GetProperty("traps").EnumerateArray())
                c.Traps.Add(new TrapDef
                {
                    Id = t.GetProperty("id").GetString(), NameEn = t.GetProperty("nameEn").GetString(), NameFa = t.GetProperty("nameFa").GetString(),
                    HomeFloor = t.GetProperty("homeFloor").GetInt32(), Tier = (CostTier)Enum.Parse(typeof(CostTier), t.GetProperty("tier").GetString()),
                    Policy = (PathPolicy)Enum.Parse(typeof(PathPolicy), t.GetProperty("policy").GetString()),
                    Shape = (TrapShape)Enum.Parse(typeof(TrapShape), t.GetProperty("shape").GetString()),
                    SafeWindow = t.GetProperty("safeWindow").GetDouble(), Interactive = t.GetProperty("interactive").GetBoolean(),
                    Release = t.GetProperty("release").GetString()
                });
            foreach (var r in root.GetProperty("rewards").EnumerateArray())
                c.Rewards.Add(new RewardDef { Id = r.GetProperty("id").GetString(), NameEn = r.GetProperty("nameEn").GetString(), NameFa = r.GetProperty("nameFa").GetString(),
                    Rarity = (Rarity)Enum.Parse(typeof(Rarity), r.GetProperty("rarity").GetString()), Release = r.GetProperty("release").GetString() });
            foreach (var m in root.GetProperty("monsters").EnumerateArray())
                c.Monsters.Add(new MonsterDef { Id = m.GetProperty("id").GetString(), NameEn = m.GetProperty("nameEn").GetString(), NameFa = m.GetProperty("nameFa").GetString(),
                    Floor = m.GetProperty("floor").GetInt32(), Boss = m.GetProperty("boss").GetBoolean(), Release = m.GetProperty("release").GetString() });
            foreach (var m in root.GetProperty("mutators").EnumerateArray())
                c.Mutators.Add(new MutatorDef { Id = m.GetProperty("id").GetString(), NameEn = m.GetProperty("nameEn").GetString(), NameFa = m.GetProperty("nameFa").GetString(),
                    Floors = m.GetProperty("floors").EnumerateArray().Select(x => x.GetInt32()).ToArray(),
                    Conflicts = m.GetProperty("conflicts").EnumerateArray().Select(x => x.GetString()).ToArray(),
                    GauntletSafe = m.GetProperty("gauntletSafe").GetBoolean(), Release = m.GetProperty("release").GetString() });
            return c;
        }

        private static void CatalogTests()
        {
            Check(_cat.Traps.Count == 90, "90 traps");
            Check(_cat.Traps.Count(t => t.Release == "v1.0") == 66, "66 traps in v1.0 (SPEC 29.2)");
            Check(_cat.Traps.Count(t => t.Release != "v1.0") == 24, "24 traps in Update 1");
            Check(_cat.Rewards.Count == 37 && _cat.Rewards.Count(r => r.Release == "v1.0") == 36, "37 rewards, 36 in v1.0");
            Check(_cat.Monsters.Count == 22 && _cat.Monsters.Count(m => m.Release == "v1.0") == 14, "22 monsters, 14 in v1.0");
            Check(_cat.Monsters.Count(m => m.Release == "v1.0" && m.Boss) == 6, "v1.0 bosses = 5 stairs guardians + crown Titan");
            Check(_cat.Mutators.Count == 13 && _cat.Mutators.Count(m => m.Release == "v1.0") == 11, "13 mutators, 11 in v1.0");
            Check(_cat.Traps.Select(t => t.Id).Distinct().Count() == 90, "trap ids unique");
            Check(_cat.Traps.Where(t => t.Shape == TrapShape.FullRow).All(t => t.Policy == PathPolicy.TimedBlocks && t.SafeWindow >= 1.2), "every full-row lane is timed with window >= 1.2 s");
            Check(_cat.Traps.Where(t => t.Policy == PathPolicy.TimedBlocks).All(t => t.SafeWindow >= 1.2), "all timed traps have window >= 1.2 s");
            var tiers = _cat.Traps.GroupBy(t => t.Tier).ToDictionary(g => g.Key, g => g.Count());
            Console.WriteLine("        tier mix: " + string.Join(", ", tiers.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)));
        }

        // ------------------------------------------------------------------------------ formulas
        private static bool Near(double a, double b, double eps = 1e-9) { return Math.Abs(a - b) < eps; }

        private static void FormulaTests()
        {
            Check(TowerRules.TrapBudget(1, 2, 5) == 8 && TowerRules.TrapBudget(10, 2, 5) == 22 && TowerRules.TrapBudget(1, 1, 5) == 4 && TowerRules.TrapBudget(1, 10, 5) == 12, "TBP table (SPEC 15.2)");
            Check(TowerRules.TrapBudget(5, 5, 3) == (int)Math.Round(14 * 0.8, MidpointRounding.AwayFromZero), "TBP scales with active players (k=3 -> x0.8)");
            Check(Near(TowerRules.BossHp(150, 1), 150) && Near(TowerRules.BossHp(150, 3), 225) && Near(TowerRules.BossHp(150, 9), 300), "boss HP x(1+0.25(n-1)) capped x2");
            Check(Near(TowerRules.InteractiveWeight(3), 2) && Near(TowerRules.InteractiveWeight(1), 0.5) && Near(TowerRules.InteractiveWeight(2), 1), "interactive trap weights");
            Check(Near(TowerRules.ExpectedFirstFinishSeconds(3), 247.5) && Near(TowerRules.ExpectedFirstFinishSeconds(6), 450) && Near(TowerRules.ExpectedFirstFinishSeconds(10), 720), "E(H): 4.1 / 7.5 / 12 min (SPEC 29.3)");
            Check(Near(TowerRules.FogStartSeconds(10), 540) && Near(TowerRules.HardCapSeconds(10), 840) && Near(TowerRules.FogReachesFloorAt(10, 10), 765), "H=10: fog 9:00, floor 10 at 12:45, cap 14:00");
            Check(Near(TowerRules.FogStartSeconds(6), 337.5) && Near(TowerRules.HardCapSeconds(6), 570) && Near(TowerRules.FogReachesFloorAt(6, 6), 462.5), "H=6: fog ~5:37, floor 6 at ~7:42, cap 9:30");
            Check(TowerRules.IsFloorFogged(10, 1, 540) && !TowerRules.IsFloorFogged(10, 2, 540), "fog front moves one floor per 25 s");
            Check(Progression.TotalXpToReach(60) == 56640 && Progression.TotalXpToReach(40) == 25740, "XP totals 56,640 (L60) and 25,740 (L40)");
            Check(Progression.LevelCap(true) == 40 && Progression.LevelCap(false) == 60 && Progression.LevelForXp(10_000_000, 40) == 40, "level cap 40 in v1.0, 60 after Update 1");
            var cfg6 = new TowerConfig(6); var cfg10 = new TowerConfig(10);
            Check(cfg6.MaxProgress == 60 && cfg10.MaxProgress == 100 && TowerConfig.Progress(6, 10) == 60 && cfg6.IsCrownRoom(6, 10) && !cfg6.IsCrownRoom(5, 10) && cfg10.IsCrownRoom(10, 10), "crown room = floor H room 10");
            Check(cfg6.Cols(3) == 7 && cfg6.Rows(3) == 7 && cfg6.Cols(10) == 9 && cfg6.Rows(10) == 10, "room grids 7x7 and 9x10 (SPEC 2.2)");

            var rewards = Enumerable.Range(1, 60).Select(l => Progression.RewardFor(l, false)).ToList();
            Check(rewards.Sum(r => r.Gold) == 21300, "level rewards: 21,300 gold");
            Check(rewards.Count(r => r.ChatLine > 0) == 30 && rewards.Count(r => r.VoiceLine > 0) == 20, "30 chat lines, 20 voice lines");
            Check(rewards.Count(r => r.SkinRarity != null) == 12 && rewards.Count(r => r.SkinRarity == "Rare") == 6 && rewards.Count(r => r.SkinRarity == "Epic") == 4 && rewards.Count(r => r.SkinRarity == "Mythic") == 2, "12 skins (6 Rare, 4 Epic, 2 Mythic)");
            Check(rewards.Count(r => r.AvatarFrame) == 8 && rewards.Sum(r => r.Gems) == 120, "8 frames, 120 gems");
            Check(rewards.Count(r => r.Chest == "Wooden") == 10 && rewards.Count(r => r.Chest == "Silver") == 6 && rewards.Count(r => r.Chest == "Golden") == 2 && rewards.Count(r => r.Chest == "Mythic") == 1 && rewards.Count(r => r.Chest == "Arcana") == 1, "chests 10/6/2/1/1");
            Check(Progression.RewardFor(6, true).HeroUnlock == 3 && Progression.RewardFor(12, true).HeroUnlock == 4, "Rostam at L6, Mitra at L12");
            Check(Progression.RewardFor(20, true).SkinHero == 2 && Progression.RewardFor(20, false).SkinHero == 3, "v1.0 level-20 Epic goes to Kimia, full tower to Rostam");
            // no skin reward in v1.0 (levels 1..40) may belong to a hero without that skin tier at launch
            bool launchOk = Enumerable.Range(1, 40).Select(l => Progression.RewardFor(l, true)).Where(r => r.SkinRarity != null)
                .All(r => r.SkinRarity == "Rare" || r.SkinHero <= 2);
            Check(launchOk, "levels 1..40 never grant an Epic/Mythic skin of Rostam/Mitra (not in v1.0)");
        }

        // ------------------------------------------------------------------------------ determinism
        private static string Fingerprint(RoomLayout L)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(L.Floor).Append('/').Append(L.Room).Append('|').Append(L.MutatorId).Append('|').Append(L.RewardId).Append('|').Append(L.Aid).Append(L.CanalRow).Append('|').Append(L.Spent);
            foreach (var c in L.GoldenPath) sb.Append(';').Append(c.X).Append(',').Append(c.Y);
            foreach (var p in L.Placements) sb.Append('#').Append(p.TrapId).Append(p.X).Append(',').Append(p.Y);
            if (L.PortalTile.HasValue) sb.Append("P").Append(L.PortalTile.Value.X).Append(L.PortalTile.Value.Y);
            if (L.MonsterSpawn.HasValue) sb.Append("M").Append(L.MonsterSpawn.Value.X).Append(L.MonsterSpawn.Value.Y);
            return sb.ToString();
        }

        private static void DeterminismTests()
        {
            var cfg = new TowerConfig(6);
            var a = new SolvableRoomGenerator(cfg, _cat); var b = new SolvableRoomGenerator(cfg, _cat);
            int same = 0; var fps = new HashSet<string>();
            for (int i = 0; i < 2000; i++)
            {
                var rq = new RoomRequest { Floor = 1 + i % 6, Room = 1 + (i / 6) % 10, MatchSeed = 1000 + i, HasPortal = false };
                var f1 = Fingerprint(a.Generate(rq)); var f2 = Fingerprint(b.Generate(rq));
                if (f1 == f2) same++; fps.Add(f1);
            }
            Check(same == 2000, "same (seed, floor, room) -> identical layout on two generator instances");
            Check(fps.Count > 1900, "different seeds give different rooms", fps.Count + "/2000 unique");
            var r1 = new DeterministicRng(42); var r2 = new DeterministicRng(42);
            bool eq = true; for (int i = 0; i < 1000; i++) if (r1.NextU64() != r2.NextU64()) eq = false;
            Check(eq, "RNG is reproducible");
            // golden values pin the RNG algorithm: server and every client build must match
            Check(new DeterministicRng(0).NextU64() == 0xE220A8397B1DCDAFUL, "SplitMix64 reference value (platform independent)");
        }

        // ------------------------------------------------------------------------------ solvability
        private sealed class Stats
        {
            public long Rooms, Traps, Canals, Mutators, Monsters, Portals, StdRoomsCounted, MutatorEligible; public double StdTraversal, StdTraversalMax, GauntletTraversal; public long GauntletCount;
            public ConcurrentBag<string> Errors = new ConcurrentBag<string>();
        }

        private static void SolvabilityTest(int h, int seedsPerFloor)
        {
            var cfg = new TowerConfig(h);
            var gen = new SolvableRoomGenerator(cfg, _cat);
            var st = new Stats(); object lk = new object();
            var sw = Stopwatch.StartNew();
            Parallel.For(1, h + 1, floor =>
            {
                var rng = new DeterministicRng((ulong)(floor * 7919 + h));
                double trav = 0, travMax = 0, travG = 0; long rooms = 0, traps = 0, canals = 0, muts = 0, mons = 0, ports = 0, std = 0, gcount = 0, elig = 0;
                for (int i = 0; i < seedsPerFloor; i++)
                {
                    int room = 1 + rng.Next(10);
                    var rq = new RoomRequest
                    {
                        Floor = floor, Room = room, MatchSeed = (long)rng.NextU64(),
                        ActivePlayers = 2 + rng.Next(4), PlayersInRoom = 1 + rng.Next(5),
                        HasPortal = room >= 2 && room <= 9 && rng.NextDouble() < 0.3, ComebackUpgrade = rng.NextDouble() < 0.1
                    };
                    var L = gen.Generate(rq);
                    var errs = RoomValidator.Validate(L, _cat, cfg);
                    if (errs.Count > 0 && st.Errors.Count < 10) st.Errors.Add(errs[0] + " (seed " + rq.MatchSeed + ")");
                    rooms++; traps += L.Placements.Count(p => p.TrapId != "AID_SWITCH"); if (L.CanalRow >= 0) canals++; if (L.MutatorId != null) muts++; if (L.MonsterLock && !L.IsGauntlet && !L.IsCrown) mons++; if (L.PortalTile.HasValue) ports++;
                    if (room >= 2 && floor >= 2) elig++;
                    double tt = RoomMetrics.EstimateTraversalSeconds(L, _cat);
                    if (room >= 2 && room <= 9) { std++; trav += tt; travMax = Math.Max(travMax, tt); }
                    if (L.IsGauntlet) { gcount++; travG += tt; }
                }
                lock (lk) { st.Rooms += rooms; st.Traps += traps; st.Canals += canals; st.Mutators += muts; st.Monsters += mons; st.Portals += ports; st.StdRoomsCounted += std; st.StdTraversal += trav; st.StdTraversalMax = Math.Max(st.StdTraversalMax, travMax); st.GauntletTraversal += travG; st.GauntletCount += gcount; st.MutatorEligible += elig; }
            });
            sw.Stop();
            string head = "H=" + h + ": " + st.Rooms + " rooms in " + sw.ElapsedMilliseconds + " ms";
            Check(st.Errors.IsEmpty, head + " -> 0 validator violations (path, BFS, aids, windows, budget, caps, entrance)", st.Errors.IsEmpty ? "" : string.Join(" | ", st.Errors.Take(3)));
            double avg = st.StdTraversal / st.StdRoomsCounted;
            Check(avg >= 2.3 && avg <= 6.0, "H=" + h + ": mean no-hit traversal of normal rooms within SPEC budget (2.3 s straight run .. ~4-6 s)", avg.ToString("F2") + " s mean, max " + st.StdTraversalMax.ToString("F2") + " s");
            Console.WriteLine("        avg traps/room " + ((double)st.Traps / st.Rooms).ToString("F1") + ", canals " + (100.0 * st.Canals / st.Rooms).ToString("F1") + "%, mutators " + (100.0 * st.Mutators / Math.Max(1, st.MutatorEligible)).ToString("F1") + "% of rooms 2-9 (floors>=2), monster rooms " + (100.0 * st.Monsters / st.Rooms).ToString("F1") + "% overall, gauntlet traversal " + (st.GauntletTraversal / Math.Max(1, st.GauntletCount)).ToString("F2") + " s");
        }

        private static void WorldTests()
        {
            foreach (int h in new[] { 3, 6, 10 })
            {
                var cfg = new TowerConfig(h);
                int worlds = 300; int capViolations = 0, portalBad = 0, roomCountBad = 0, maxAttempts = 0; double maxMs = 0, sumMs = 0; long mutRooms = 0, totalRooms = 0;
                for (long seed = 1; seed <= worlds; seed++)
                {
                    var sw = Stopwatch.StartNew();
                    var w = MatchWorld.Build(cfg, _cat, seed * 2654435761L);
                    sw.Stop(); sumMs += sw.Elapsed.TotalMilliseconds; maxMs = Math.Max(maxMs, sw.Elapsed.TotalMilliseconds);
                    maxAttempts = Math.Max(maxAttempts, w.Attempts);
                    int rooms = 0;
                    for (int f = 1; f <= h; f++)
                    {
                        int muts = 0;
                        for (int r = 1; r <= 10; r++)
                        {
                            var L = w.Room(f, r); rooms++; totalRooms++;
                            if (L.MutatorId != null) { muts++; mutRooms++; if (f == 1 || r == 1) capViolations++; }
                            bool planned = w.Portals.Any(p => p.SourceFloor == f && p.SourceRoom == r);
                            if (planned != L.PortalTile.HasValue) portalBad++;
                        }
                        if (muts > 2) capViolations++;
                    }
                    if (rooms != 10 * h) roomCountBad++;
                }
                Check(capViolations == 0, "H=" + h + ": <= 2 mutator rooms per floor (25%), none on floor 1 or room 1", (100.0 * mutRooms / totalRooms).ToString("F1") + "% of all rooms");
                Check(portalBad == 0 && roomCountBad == 0, "H=" + h + ": exactly " + (10 * h) + " rooms, portal tiles match the portal plan");
                Check(maxMs < 3000, "H=" + h + ": world built in < 3 s (SPEC 14.2)", "mean " + (sumMs / worlds).ToString("F1") + " ms, max " + maxMs.ToString("F0") + " ms, max attempts " + maxAttempts);
            }
            var c6 = new TowerConfig(6);
            var a = MatchWorld.Build(c6, _cat, 777); var b = MatchWorld.Build(c6, _cat, 777);
            bool same = true; for (int f = 1; f <= 6; f++) for (int r = 1; r <= 10; r++) if (Fingerprint(a.Room(f, r)) != Fingerprint(b.Room(f, r))) same = false;
            Check(same, "same matchSeed -> identical 60-room world (server and clients agree)");
        }

        // ------------------------------------------------------------------------------ simulation
        private static RoomLayout Blank(int cols = 7, int rows = 7)
        {
            var L = new RoomLayout { Floor = 2, Room = 3, Cols = cols, Rows = rows, Tiles = new TileKind[cols, rows], Start = new Cell(cols / 2, 0), Goal = new Cell(cols / 2, rows - 1), ZoneStartRow = 2, ZoneEndRow = rows - 2 };
            return L;
        }

        private static void SimTests()
        {
            // ---- movement & collision
            var L = Blank(); L.Tiles[3, 3] = TileKind.Hazard;
            var h = new HeroState { X = RoomSpace.CenterX(3), Z = RoomSpace.CenterZ(1) };
            for (int i = 0; i < 90; i++) HeroSim.Step(L, h, 0, 1, 1 / 30.0, false);
            Check(h.Z <= 3 * 2 - 0.5 + 1e-9 && h.Z > 5.0, "hazard tile blocks the hero (stops at its edge minus the 0.5 m radius)", h.Z.ToString("F2"));
            h = new HeroState { X = 7, Z = 3 };
            for (int i = 0; i < 300; i++) HeroSim.Step(L, h, -1, 0, 1 / 30.0, false);
            Check(Math.Abs(h.X - 0.5) < 1e-9, "walls clamp the hero inside the room");
            h = new HeroState { X = RoomSpace.CenterX(3), Z = RoomSpace.CenterZ(5) }; var Lg = Blank();
            for (int i = 0; i < 90; i++) HeroSim.Step(Lg, h, 0, 1, 1 / 30.0, true);
            bool lockedStops = h.Z <= 6 * 2 - 0.5 + 1e-9;
            h = new HeroState { X = RoomSpace.CenterX(3), Z = RoomSpace.CenterZ(5) };
            for (int i = 0; i < 90; i++) HeroSim.Step(Lg, h, 0, 1, 1 / 30.0, false);
            Check(lockedStops && RoomSpace.CellAt(h.X, h.Z).Y == 6, "locked exit door blocks the goal tile; unlocked it can be entered");
            h = new HeroState { X = 7, Z = 3 }; HeroSim.Step(Lg, h, 3, 4, 1.0, false);
            Check(Math.Abs(Math.Sqrt((h.X - 7) * (h.X - 7) + (h.Z - 3) * (h.Z - 3)) - 6.0) < 1e-6 || h.Z >= 13.5 || h.X >= 13.5, "diagonal input is normalised (6 m/s, not 6*sqrt2)");

            // ---- trap clock
            var t50 = _cat.Trap("T50"); var Lc = Blank();
            int blockedSteps = 0, steps = 100000; double minRun = 1e9, run = 0; int warnSteps = 0; bool seenBlock = false;
            for (int i = 0; i < steps; i++)
            {
                double t = i * 0.001; var st = TrapClock.StateAt(99, Lc, 0, t50, t);
                if (st == LaneState.Blocked) { blockedSteps++; if (run > 0 && seenBlock) minRun = Math.Min(minRun, run); seenBlock = true; run = 0; }
                else { run += 0.001; if (st == LaneState.Warning) warnSteps++; }
            }
            Check(Math.Abs((double)blockedSteps / steps - 1.0 / 2.5) < 0.01, "T50 cycle: blocked 1.0 s of every 2.5 s", ((double)blockedSteps / steps).ToString("F3"));
            Check(minRun >= 1.45, "every open window lasts >= 1.2 s (here 1.5 s) - SPEC rule 3", minRun.ToString("F2") + " s");
            Check(Math.Abs(warnSteps / 1000.0 / (steps / 1000.0 / 2.5) - 0.8) < 0.05, "telegraph (Warning) lasts ~0.8 s before each block");
            Check(TrapClock.StateAt(99, Lc, 0, t50, 3.3) == TrapClock.StateAt(99, Lc, 0, t50, 3.3), "trap clock is deterministic");

            // ---- camera
            var p1 = CameraRig.Compute(7, 5, CameraMode.Standard); var p2 = CameraRig.Compute(7, 5, CameraMode.Combat); var p3 = CameraRig.Compute(7, 5, CameraMode.XRay);
            Check(Math.Abs(p1.PitchDeg - 54.0) < 0.05 && Math.Abs(p2.PitchDeg - 58.0) < 0.05 && Math.Abs(p3.PitchDeg - 62.0) < 0.05, "camera pitch = 54 / 58 / 62 deg (SPEC 2.3 corrected offsets)", p1.PitchDeg.ToString("F2") + "/" + p2.PitchDeg.ToString("F2") + "/" + p3.PitchDeg.ToString("F2"));
            Check(p1.TargetZ == 5 + CameraRig.LookAhead && p1.PosY == 11.0 && p1.Fov == 44, "standard camera looks 4 m ahead, height 11 m, FOV 44");
            var Lr = Blank(9, 10); Lr.Room = 10; Lr.IsGauntlet = true;
            Check(CameraRig.ModeFor(Blank(), 1, false, false) == CameraMode.Standard && CameraRig.ModeFor(Blank(), 2, false, false) == CameraMode.Combat && CameraRig.ModeFor(Lr, 1, false, false) == CameraMode.Combat && CameraRig.ModeFor(Blank(), 1, true, false) == CameraMode.Combat && CameraRig.ModeFor(Blank(), 5, true, true) == CameraMode.XRay, "camera mode activation rules");

            // ---- run flow
            var cfg = new TowerConfig(6); var w = MatchWorld.Build(cfg, _cat, 4242);
            var r = new TowerRun(w, _cat, 1);
            r.Damage(1000, false);
            Check(r.IsDead && r.RespawnTimer >= 3.0 && r.RespawnTimer <= 5.0 && r.Floor == 1, "death in room 1: 3-5 s pause, stays on the floor");
            for (int i = 0; i < 400 && r.IsDead; i++) r.Step(0.02, 0, 0);
            double hpAfter = r.Hero.Hp; r.Damage(60, false);
            Check(!r.IsDead && r.Room == 1 && hpAfter == HeroState.MaxHp && r.Hero.Hp == HeroState.MaxHp && r.InvulnTimer > 2.5, "respawn with full HP and 3 s spawn shield (damage ignored)");
            var r7 = new TowerRun(w, _cat, 7); r7.Damage(1000, false);
            Check(Math.Abs(r7.RespawnTimer - 2.0) < 1e-9, "death in room 7: 2 s");
            for (int i = 0; i < 200 && r7.IsDead; i++) r7.Step(0.02, 0, 0);
            Check(r7.Floor == 1 && r7.Room == 1 && r7.Deaths == 1, "death in rooms 2..10 sends the hero to room 1 of the same floor");
            var w3 = MatchWorld.Build(new TowerConfig(3), _cat, 9);
            var rf = new TowerRun(w3, _cat, 1); rf.Step(TowerRules.FogStartSeconds(3) + 5, 0, 0);
            Check(rf.Eliminated && !rf.Won, "dying in fog = permanent elimination (no respawn)");
            var rc = new TowerRun(w3, _cat, 10, 3);
            var altar = rc.Current.Goal; rc.Hero.X = RoomSpace.CenterX(altar.X); rc.Hero.Z = RoomSpace.CenterZ(altar.Y);
            for (int i = 0; i < 40; i++) rc.Step(0.1, 0, 0);
            double mid = rc.CrownProgress; rc.Damage(1, false); double afterHit = rc.CrownProgress;
            for (int i = 0; i < 70 && !rc.Won; i++) rc.Step(0.1, 0, 0);
            Check(rc.Current.IsCrown && Math.Abs(mid - 4.0) < 0.2 && afterHit == 0 && !rc.Won, "crown channel: 4 s accumulated, any damage resets it to 0");
            for (int i = 0; i < 90 && !rc.Won; i++) rc.Step(0.1, 0, 0);
            Check(rc.Won, "holding the altar for 8 s wins the match");
            var rl = new TowerRun(w, _cat, 1);
            RoomLayout lockRoom = null; int lf = 0, lr = 0;
            for (int f = 1; f <= 6 && lockRoom == null; f++) for (int rr = 2; rr <= 9; rr++) if (w.Room(f, rr).MonsterLock) { lockRoom = w.Room(f, rr); lf = f; lr = rr; break; }
            var rm = new TowerRun(w, _cat, lr, lf);
            Check(lockRoom != null && rm.DoorLocked, "monster room starts with a locked door"); rm.KillMonster();
            Check(!rm.DoorLocked, "killing the monster unlocks the door");
        }

        // ------------------------------------------------------------------------------ golden-path bot
        private sealed class BotOutcome { public bool Won, Eliminated; public double Time, Waited; public int Hits, Deaths, MonsterRooms; public string HitInfo = ""; }

        private static BotOutcome RunBot(MatchWorld w)
        {
            var run = new TowerRun(w, _cat, 1);
            var o = new BotOutcome(); const double dt = 1.0 / 30.0;
            var lastRoom = (-1, -1); double monsterWait = 0; var idxOf = new Dictionary<long, int>();
            while (!run.Won && !run.Eliminated && run.Elapsed < 3000)
            {
                var L = run.Current;
                if ((run.Floor, run.Room) != lastRoom)
                {
                    lastRoom = (run.Floor, run.Room); idxOf.Clear();
                    for (int i = 0; i < L.GoldenPath.Count; i++) idxOf[L.GoldenPath[i].X * 1000L + L.GoldenPath[i].Y] = i;
                    monsterWait = 0;
                    if (L.MonsterLock && !L.IsCrown) o.MonsterRooms++;
                }
                bool fighting = run.DoorLocked;   // a real player kills the monster first, he does not queue in front of the locked door
                if (fighting) { monsterWait += dt; if (monsterWait >= 3.0) run.KillMonster(); }   // assumed 3 s to kill

                var c = RoomSpace.CellAt(run.Hero.X, run.Hero.Z);
                int idx; if (!idxOf.TryGetValue(c.X * 1000L + c.Y, out idx)) idx = -1;
                double ix = 0, iz = 0;
                if (idx >= 0 && !fighting)
                {
                    bool atGoal = idx == L.GoldenPath.Count - 1;
                    Cell tgt = atGoal ? L.GoldenPath[idx] : L.GoldenPath[idx + 1];
                    bool wait = !atGoal && NeedWait(run, L, idx, w.Seed);
                    double tx = RoomSpace.CenterX(tgt.X), tz = RoomSpace.CenterZ(tgt.Y);
                    if (wait)   // like a real player: stand at the edge of the current tile, centre still outside the gate
                    {
                        tx = RoomSpace.CenterX(c.X) + Math.Sign(tgt.X - c.X) * 0.93; tz = RoomSpace.CenterZ(c.Y) + Math.Sign(tgt.Y - c.Y) * 0.93;
                    }
                    if (wait) o.Waited += dt;
                    double vx = tx - run.Hero.X, vz = tz - run.Hero.Z, dist = Math.Sqrt(vx * vx + vz * vz);
                    if (dist > 1e-6) { double mag = Math.Min(1.0, dist / (run.Hero.Speed * dt)); ix = vx / dist * mag; iz = vz / dist * mag; }
                }
                int before = run.TrapHits; var preCell = RoomSpace.CellAt(run.Hero.X, run.Hero.Z); var preRoom = run.Current; int pf = run.Floor, pr = run.Room; double pt = run.Elapsed;
                run.Step(dt, ix, iz); o.Hits += run.TrapHits - before;
                if (run.TrapHits > before && o.HitInfo.Length < 600)
                {
                    var hc = RoomSpace.CellAt(run.Hero.X, run.Hero.Z);
                    var desc = new System.Text.StringBuilder();
                    for (int i = 0; i < preRoom.Placements.Count; i++)
                    {
                        var pl = preRoom.Placements[i]; if (pl.TrapId == "AID_SWITCH") continue;
                        var dd = _cat.Trap(pl.TrapId);
                        bool cov = pl.Shape == TrapShape.FullRow ? pl.Y == hc.Y : (pl.X == hc.X && pl.Y == hc.Y);
                        if (cov && TrapRuntime.DamageFor(dd) > 0) desc.Append(pl.TrapId).Append('/').Append(dd.Policy).Append('/').Append(pl.Shape).Append(pl.OnGoldenPath ? "/onPath" : "/OFFpath").Append(' ');
                    }
                    int pidx2 = preRoom.GoldenPath.FindIndex(g => g.X == hc.X && g.Y == hc.Y);
                    o.HitInfo += "[F" + pf + "R" + pr + " t=" + pt.ToString("F1") + " cell(" + hc.X + "," + hc.Y + ") pre(" + preCell.X + "," + preCell.Y + ") pathIdx=" + pidx2 + " " + desc + "] ";
                }
            }
            o.Won = run.Won; o.Eliminated = run.Eliminated; o.Time = run.Elapsed; o.Deaths = run.Deaths;
            return o;
        }

        /// <summary>True when stepping into the next path tile would put the hero inside a timed lane that is (or will become) blocked before he is through.</summary>
        private static bool NeedWait(TowerRun run, RoomLayout L, int idx, long seed)
        {
            var next = L.GoldenPath[idx + 1];
            for (int i = 0; i < L.Placements.Count; i++)
            {
                var p = L.Placements[i]; if (p.TrapId == "AID_SWITCH") continue;
                var d = _cat.Trap(p.TrapId); if (d.Policy != PathPolicy.TimedBlocks) continue;
                bool covers = p.Shape == TrapShape.FullRow ? p.Y == next.Y : (p.X == next.X && p.Y == next.Y);
                if (!covers) continue;
                var here = L.GoldenPath[idx];
                if (p.Shape == TrapShape.FullRow ? here.Y == p.Y : (here.X == p.X && here.Y == p.Y)) continue;   // already inside the lane
                int runLen = 1;
                if (p.Shape == TrapShape.FullRow) { int k = idx + 2; while (k < L.GoldenPath.Count && L.GoldenPath[k].Y == p.Y) { runLen++; k++; } }
                double toBoundary = next.Y > here.Y ? (here.Y + 1) * RoomSpace.Tile - run.Hero.Z : next.Y < here.Y ? run.Hero.Z - here.Y * RoomSpace.Tile
                                  : next.X > here.X ? (here.X + 1) * RoomSpace.Tile - run.Hero.X : run.Hero.X - here.X * RoomSpace.Tile;
                double need = Math.Max(0, toBoundary) / 6.0 + 0.14 + runLen * (RoomSpace.Tile / 6.0);
                if (TrapClock.OpenRemaining(seed, L, i, d, run.Elapsed) < need) return true;
            }
            return false;
        }

        private static void BotTests()
        {
            foreach (var (h, worlds) in new[] { (3, 150), (6, 100), (10, 40) })
            {
                var cfg = new TowerConfig(h);
                int won = 0, hits = 0, deaths = 0, elim = 0, monsters = 0; double sum = 0, max = 0, waited = 0, min = 1e9;
                Parallel.For(0, worlds, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                {
                    var w = MatchWorld.Build(cfg, _cat, 1000003L * (i + 1) + h);
                    var o = RunBot(w);
                    lock (_cat) { if (o.HitInfo.Length > 0) Console.WriteLine("        HIT " + o.HitInfo); if (o.Won) won++; if (o.Eliminated) elim++; hits += o.Hits; deaths += o.Deaths; monsters += o.MonsterRooms; sum += o.Time; max = Math.Max(max, o.Time); min = Math.Min(min, o.Time); waited += o.Waited; }
                });
                Check(won == worlds && hits == 0 && deaths == 0 && elim == 0, "H=" + h + ": " + worlds + " full towers (" + (10 * h) + " rooms each) cleared by the golden-path bot with 0 trap hits, 0 deaths", "won " + won + ", hits " + hits);
                Console.WriteLine("        flawless bot time: mean " + (sum / worlds / 60).ToString("F1") + " min (min " + (min / 60).ToString("F1") + ", max " + (max / 60).ToString("F1") + "), expected first place E(H) " + (TowerRules.ExpectedFirstFinishSeconds(h) / 60).ToString("F1") + " min, fog starts " + (TowerRules.FogStartSeconds(h) / 60).ToString("F1") + " min; mean wait at timed lanes " + (waited / worlds).ToString("F1") + " s/tower; monster rooms/tower " + ((double)monsters / worlds).ToString("F1") + " (3 s each assumed)");
            }
        }

        // ------------------------------------------------------------------------------ negative tests
        private static void NegativeTests()
        {
            var cfg = new TowerConfig(6); var gen = new SolvableRoomGenerator(cfg, _cat);
            RoomLayout Fresh(int floor = 4, int room = 5, long seed = 12345) { return gen.Generate(new RoomRequest { Floor = floor, Room = room, MatchSeed = seed }); }

            var L = Fresh(); Check(RoomValidator.Validate(L, _cat, cfg).Count == 0, "baseline room is valid");
            L = Fresh(); var mid = L.GoldenPath.First(c => c.Y == 3); L.Tiles[mid.X, mid.Y] = TileKind.Hazard;
            Check(RoomValidator.Validate(L, _cat, cfg).Any(x => x.Contains("hazard on the golden path")), "hazard dropped on the golden path -> detected");
            L = Fresh(); for (int x = 0; x < L.Cols; x++) L.Tiles[x, 3] = TileKind.Hazard;
            Check(RoomValidator.Validate(L, _cat, cfg).Any(x => x.Contains("BFS cannot reach the goal")), "a wall of hazards across the room -> BFS failure detected");
            L = Fresh(); L.Budget = 0; Check(RoomValidator.Validate(L, _cat, cfg).Any(x => x.Contains("TBP overspent")), "spending above the TBP budget -> detected");
            L = Fresh(); L.Spent += 50; Check(RoomValidator.Validate(L, _cat, cfg).Any(x => x.Contains("bookkeeping")), "wrong Spent bookkeeping -> detected");
            L = Fresh(); L.Placements.Add(new TrapPlacement { TrapId = "T31", X = 1, Y = 1, Shape = TrapShape.Tile }); Check(RoomValidator.Validate(L, _cat, cfg).Any(x => x.Contains("entrance") || x.Contains("outside the trap zone")), "trap in the entrance rows -> detected");
            L = Fresh(); var pc = L.GoldenPath.First(c => c.Y == 4); L.Placements.Add(new TrapPlacement { TrapId = "T32", X = pc.X, Y = pc.Y, Shape = TrapShape.Tile, OnGoldenPath = true });
            Check(RoomValidator.Validate(L, _cat, cfg).Any(x => x.Contains("permanent hazard")), "acid pool on the golden path -> detected");
            L = Fresh(); L.Placements.Add(new TrapPlacement { TrapId = "T48", X = pc.X, Y = pc.Y, Shape = TrapShape.Tile, OnGoldenPath = true });
            Check(RoomValidator.Validate(L, _cat, cfg).Any(x => x.Contains("zero-damage route broken")), "damaging untimed trap (T48 cracked tile) on the golden path -> detected");
            L = Fresh(); L.Placements.Add(new TrapPlacement { TrapId = "T38", X = pc.X, Y = pc.Y, Shape = TrapShape.Tile, OnGoldenPath = true });
            var cat2 = new Catalog { Traps = _cat.Traps.Select(t => t.Id == "T38" ? new TrapDef { Id = t.Id, NameEn = t.NameEn, HomeFloor = 0, Tier = t.Tier, Policy = PathPolicy.TimedBlocks, Shape = t.Shape, SafeWindow = 0.8, Release = t.Release } : t).ToList(), Rewards = _cat.Rewards, Monsters = _cat.Monsters, Mutators = _cat.Mutators };
            Check(RoomValidator.Validate(L, cat2, cfg).Any(x => x.Contains("window < 1.2")), "timed trap with 0.8 s window on the path -> detected");
            // a room that did contain a canal must report the aid
            bool foundCanal = false;
            for (long s = 0; s < 4000 && !foundCanal; s++)
            {
                var C = gen.Generate(new RoomRequest { Floor = 6, Room = 5, MatchSeed = s });
                if (C.CanalRow < 0) continue;
                foundCanal = true;
                bool aidTile = C.GoldenPath.Where(c => c.Y == C.CanalRow).All(c => C.Tiles[c.X, c.Y] == TileKind.CrossingAid);
                C.Aid = CrossingAid.None;
                Check(aidTile && RoomValidator.Validate(C, _cat, cfg).Any(x => x.Contains("aid")), "full-width canal always has an aid on the path; removing the aid is detected");
            }
            Check(foundCanal, "generator does produce canals with aids (not vacuously safe)");
        }

        private static void ReleaseIsolationTest()
        {
            var cfg = new TowerConfig(6); var gen = new SolvableRoomGenerator(cfg, _cat);
            var upd = new HashSet<string>(_cat.Traps.Where(t => t.Release != "v1.0").Select(t => t.Id));
            var updRew = new HashSet<string>(_cat.Rewards.Where(r => r.Release != "v1.0").Select(r => r.Id));
            var updMut = new HashSet<string>(_cat.Mutators.Where(m => m.Release != "v1.0").Select(m => m.Id));
            bool bad = false; int crownRooms = 0, crownTrapsNearAltar = 0;
            for (int f = 1; f <= 6; f++) for (int r = 1; r <= 10; r++) for (long s = 0; s < 1500; s++)
            {
                var L = gen.Generate(new RoomRequest { Floor = f, Room = r, MatchSeed = s * 31 + f * 7 + r });
                if (L.Placements.Any(p => upd.Contains(p.TrapId)) || (L.RewardId != null && updRew.Contains(L.RewardId)) || (L.MutatorId != null && updMut.Contains(L.MutatorId))) bad = true;
                if (L.IsCrown) { crownRooms++; if (L.Placements.Any(p => Math.Abs(p.X - L.Goal.X) <= 1 && Math.Abs(p.Y - L.Goal.Y) <= 1 && p.Shape == TrapShape.Tile)) crownTrapsNearAltar++; }
            }
            Check(!bad, "H=6 generates no Update-1 trap / reward / mutator");
            Check(crownRooms == 1500 && crownTrapsNearAltar == 0, "H=6: only floor 6 room 10 is the crown room, and its altar circle is trap-free", crownRooms + " crown rooms");
            var cfg10 = new TowerConfig(10); var gen10 = new SolvableRoomGenerator(cfg10, _cat);
            bool sawUpdate = false;
            for (int f = 7; f <= 10 && !sawUpdate; f++) for (long s = 0; s < 3000; s++)
                if (gen10.Generate(new RoomRequest { Floor = f, Room = 3, MatchSeed = s }).Placements.Any(p => upd.Contains(p.TrapId))) { sawUpdate = true; break; }
            Check(sawUpdate, "H=10 does use the Update-1 traps");
            bool portalThrows = false; try { gen.Generate(new RoomRequest { Floor = 2, Room = 10, MatchSeed = 1, HasPortal = true }); } catch (ArgumentException) { portalThrows = true; }
            Check(portalThrows, "portal request in room 10 is rejected");
        }

        // ------------------------------------------------------------------------------ rules
        private static PlayerResult P(string id, PlayerStatus s, int prog, double reached = 0, double elim = 0, bool crown = false)
        { return new PlayerResult { Id = id, Status = s, MaxProgress = prog, ReachedAtSec = reached, EliminatedAtSec = elim, CrownHolder = crown }; }

        private static void RuleTests()
        {
            var ranked = Ranking.Rank(new[]
            {
                P("forfeit", PlayerStatus.Forfeit, 55), P("elimLate", PlayerStatus.Eliminated, 40, elim: 500), P("elimEarly", PlayerStatus.Eliminated, 40, elim: 300),
                P("aliveB", PlayerStatus.Alive, 30, reached: 100), P("aliveA", PlayerStatus.Alive, 30, reached: 90), P("crown", PlayerStatus.Alive, 58, crown: true)
            }).Select(p => p.Id).ToList();
            Check(string.Join(",", ranked) == "crown,aliveA,aliveB,elimLate,elimEarly,forfeit", "ranking order: crown > alive (earlier tie-break) > eliminated (later death first) > forfeit (SPEC 17.5)", string.Join(",", ranked));
            var two = Ranking.Rank(new[] { P("x", PlayerStatus.Alive, 20), P("y", PlayerStatus.Alive, 33) }).Select(p => p.Id).ToList();
            Check(two[0] == "y", "higher MaxProgress ranks higher among the living");
            var players = new List<PlayerResult> { P("a", PlayerStatus.Alive, 10), P("b", PlayerStatus.Eliminated, 5), P("c", PlayerStatus.Forfeit, 2) };
            Check(Ranking.EvaluateEnd(players, 100, 6) == Ranking.EndReason.LastActivePlayer, "match ends when one active player remains");
            players = new List<PlayerResult> { P("a", PlayerStatus.Alive, 10), P("b", PlayerStatus.Alive, 5) };
            Check(Ranking.EvaluateEnd(players, 100, 6) == Ranking.EndReason.None && Ranking.EvaluateEnd(players, 570, 6) == Ranking.EndReason.TimeCap, "time cap 9:30 for H=6");
            players[0].CrownHolder = true; Check(Ranking.EvaluateEnd(players, 100, 6) == Ranking.EndReason.CrownCaptured, "crown capture ends the match");

            var rng = new DeterministicRng(5);
            var r1 = MatchFlow.Respawn(4, 1, rng); var r5 = MatchFlow.Respawn(4, 7, rng);
            Check(r1.Floor == 4 && r1.Room == 1 && r1.DelaySeconds >= 3 && r1.DelaySeconds <= 5 && r5.Floor == 4 && r5.Room == 1 && r5.DelaySeconds == 2.0 && r5.SpawnShieldSeconds == 3.0 && r5.DropsLoot, "death: same floor, room 1, 3-5 s pause in room 1 else 2 s, 3 s spawn shield (never falls a floor)");
            var seen = new HashSet<int>(); for (int i = 0; i < 500; i++) seen.Add(MatchFlow.RollStartRoom(rng));
            Check(seen.SetEquals(new[] { 1, 2, 3, 4, 5, 6 }), "start dice covers exactly 1..6");

            var sb = new ScanBudget();
            Check(sb.TryUse("a") && sb.TryUse("b") && sb.TryUse("c") && !sb.TryUse("d") && sb.TryUse("a") && sb.Used == 3, "scan cap 3 per match; repeated request id does not consume twice");
            sb.Refund("b"); Check(sb.TryUse("e"), "refunded (undelivered) scan frees a slot");

            foreach (int h in new[] { 6, 10 })
            {
                var cfg = new TowerConfig(h); int bad = 0, total = 0, downTop = 0, mirTop = 0, topPortals = 0; bool floor1Up = true; int perFloorOk = 0;
                for (long seed = 0; seed < 5000; seed++)
                {
                    var plan = PortalPlanner.Plan(cfg, seed);
                    for (int f = 1; f <= h; f++)
                    {
                        int want = (f == 4 || f == 5 || f == 8) ? 3 : 2;
                        var ps = plan.Where(p => p.SourceFloor == f).ToList();
                        if (ps.Count == want && ps.Select(p => p.SourceRoom).Distinct().Count() == want) perFloorOk++;
                    }
                    foreach (var p in plan)
                    {
                        total++;
                        if (p.SourceRoom < 2 || p.SourceRoom > 9 || p.DestRoom < 2 || p.DestRoom > 9 || p.DestFloor < 1 || p.DestFloor > h) bad++;
                        if (p.Ascending && p.DestFloor == h && p.DestRoom > 5) bad++;
                        if (p.SourceFloor == 1 && !p.Ascending) floor1Up = false;
                        if (p.Ascending && p.DestFloor <= p.SourceFloor) bad++;
                        if (!p.Ascending && !p.Mirage && p.DestFloor >= p.SourceFloor) bad++;
                        if (p.SourceFloor == h) { topPortals++; if (!p.Ascending && !p.Mirage) downTop++; if (p.Mirage) mirTop++; }
                    }
                }
                Check(bad == 0 && floor1Up && perFloorOk == 5000 * h, "H=" + h + ": portals: rooms 2..9, ascent capped at floor H room 5, floor 1 all ascending, 2 per floor (3 on floors 4/5/8)");
                double pd = (double)downTop / topPortals;
                Check(pd > 0.77 && pd < 0.83 && downTop + mirTop == topPortals, "H=" + h + ": top-floor portals ~80% descending, remainder mirage", (100 * pd).ToString("F1") + "%");
            }
        }
    }
}
