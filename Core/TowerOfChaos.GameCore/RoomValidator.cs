using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerOfChaos.GameCore
{
    /// <summary>
    /// Independent re-check of every guarantee in SPEC 6 / 15.2 / 18.3. It does NOT trust the generator:
    /// a violation means a bug, and the match world generator must reseed (SPEC 14.2 step 4).
    /// </summary>
    public static class RoomValidator
    {
        public static List<string> Validate(RoomLayout L, Catalog cat, TowerConfig cfg)
        {
            var e = new List<string>();
            Func<string, TrapDef> def = id => cat.Traps.FirstOrDefault(t => t.Id == id);
            string tag = "F" + L.Floor + "R" + L.Room;

            // path continuity
            if (L.GoldenPath.Count == 0) { e.Add(tag + ": empty golden path"); return e; }
            if (L.GoldenPath[0].X != L.Start.X || L.GoldenPath[0].Y != L.Start.Y) e.Add(tag + ": path does not start at the entrance door");
            var last = L.GoldenPath[L.GoldenPath.Count - 1];
            if (last.X != L.Goal.X || last.Y != L.Goal.Y) e.Add(tag + ": path does not end at the goal");
            var seen = new HashSet<long>();
            for (int i = 0; i < L.GoldenPath.Count; i++)
            {
                var c = L.GoldenPath[i];
                if (c.X < 0 || c.X >= L.Cols || c.Y < 0 || c.Y >= L.Rows) { e.Add(tag + ": path out of bounds"); continue; }
                if (!seen.Add(c.X * 1000L + c.Y)) e.Add(tag + ": path revisits a tile");
                if (L.Tiles[c.X, c.Y] == TileKind.Hazard) e.Add(tag + ": hazard on the golden path");
                if (i > 0 && Math.Abs(c.X - L.GoldenPath[i - 1].X) + Math.Abs(c.Y - L.GoldenPath[i - 1].Y) != 1) e.Add(tag + ": path is not 4-connected");
            }
            Func<int, int, bool> onPath = (x, y) => seen.Contains(x * 1000L + y);

            // BFS: can a player with no items and no jump skill walk from the door to the goal?
            var vis = new bool[L.Cols, L.Rows];
            var q = new Queue<Cell>();
            q.Enqueue(L.Start); vis[L.Start.X, L.Start.Y] = true;
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            bool reached = false;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c.X == L.Goal.X && c.Y == L.Goal.Y) { reached = true; break; }
                for (int d = 0; d < 4; d++)
                {
                    int nx = c.X + dx[d], ny = c.Y + dy[d];
                    if (nx < 0 || ny < 0 || nx >= L.Cols || ny >= L.Rows || vis[nx, ny]) continue;
                    if (L.Tiles[nx, ny] == TileKind.Hazard) continue;
                    vis[nx, ny] = true; q.Enqueue(new Cell(nx, ny));
                }
            }
            if (!reached) e.Add(tag + ": BFS cannot reach the goal");

            // entrance safe zone and guardian arena
            int exitFrom = L.ZoneEndRow + 1;
            int spent = 0, tierC = 0, occupied = 0;
            foreach (var p in L.Placements)
            {
                if (p.TrapId == "AID_SWITCH")
                {
                    if (!onPath(p.X, p.Y)) e.Add(tag + ": drawbridge switch is not on the golden path");
                    continue;
                }
                var d = def(p.TrapId);
                if (d == null) { e.Add(tag + ": unknown trap " + p.TrapId); continue; }
                if (d.HomeFloor > cfg.Height) e.Add(tag + ": trap " + d.Id + " does not belong to this release");
                spent += d.Cost;
                if (d.Tier == CostTier.C) tierC++;
                occupied += p.Shape == TrapShape.FullRow ? L.Cols : 1;
                if (p.Y < L.ZoneStartRow || p.Y >= exitFrom) e.Add(tag + ": trap " + d.Id + " outside the trap zone");
                if (p.Shape == TrapShape.FullRow)
                {
                    if (d.Policy == PathPolicy.BlocksWithBypass && (L.CanalRow != p.Y || L.Aid == CrossingAid.None)) e.Add(tag + ": full-width hazard without a crossing aid");
                    if (d.Policy == PathPolicy.TimedBlocks && d.SafeWindow < TowerRules.MinSafeWindowSeconds) e.Add(tag + ": timed lane " + d.Id + " window < 1.2 s");
                }
                else if (onPath(p.X, p.Y))
                {
                    if (d.Policy == PathPolicy.BlocksWithBypass) e.Add(tag + ": permanent hazard " + d.Id + " on the golden path");
                    if (d.Policy == PathPolicy.TimedBlocks && d.SafeWindow < TowerRules.MinSafeWindowSeconds) e.Add(tag + ": timed trap " + d.Id + " window < 1.2 s");
                    if (!TowerRules.IsAllowedOnGoldenPath(d)) e.Add(tag + ": damaging/untimed trap " + d.Id + " on the golden path (zero-damage route broken)");
                    if (L.Tiles[p.X, p.Y] == TileKind.CrossingAid) e.Add(tag + ": trap placed on an aid tile");
                }
            }
            // timed gates on the path: capped and separated by at least one safe path tile
            var groups = new List<int[]>();
            var pidx = new Dictionary<long, int>();
            for (int i = 0; i < L.GoldenPath.Count; i++) pidx[L.GoldenPath[i].X * 1000L + L.GoldenPath[i].Y] = i;
            foreach (var p in L.Placements)
            {
                if (p.TrapId == "AID_SWITCH") continue;
                var d = def(p.TrapId);
                if (d == null || d.Policy != PathPolicy.TimedBlocks) continue;
                if (p.Shape == TrapShape.FullRow)
                {
                    var ix = L.GoldenPath.Select((c, i) => new { c, i }).Where(z => z.c.Y == p.Y).Select(z => z.i).ToList();
                    if (ix.Count > 0) groups.Add(new[] { ix.Min(), ix.Max() });
                }
                else { int pi; if (pidx.TryGetValue(p.X * 1000L + p.Y, out pi)) groups.Add(new[] { pi, pi }); }
            }
            if (groups.Count > TowerRules.MaxTimedGatesPerRoom(L.Floor)) e.Add(tag + ": too many timed gates on the golden path");
            groups.Sort((a, b) => a[0].CompareTo(b[0]));
            for (int i = 1; i < groups.Count; i++) if (groups[i][0] - groups[i - 1][1] < 2) e.Add(tag + ": two timed gates with no safe tile between them");

            if (spent != L.Spent) e.Add(tag + ": Spent bookkeeping mismatch " + spent + " vs " + L.Spent);
            if (spent > L.Budget) e.Add(tag + ": TBP overspent " + spent + " > " + L.Budget);
            if (tierC > TowerRules.MaxTierCPerRoom(L.Floor)) e.Add(tag + ": too many tier-C traps");
            int zoneTiles = L.Cols * (L.ZoneEndRow - L.ZoneStartRow + 1);
            if (occupied > Math.Floor(zoneTiles * TowerRules.TrapZoneMaxOccupancy)) e.Add(tag + ": occupancy above 45%");

            // canal
            if (L.CanalRow >= 0)
            {
                if (L.Aid == CrossingAid.None) e.Add(tag + ": canal without aid");
                for (int x = 0; x < L.Cols; x++)
                {
                    bool ok = onPath(x, L.CanalRow) ? L.Tiles[x, L.CanalRow] == TileKind.CrossingAid : L.Tiles[x, L.CanalRow] == TileKind.Hazard;
                    if (!ok) e.Add(tag + ": canal tile state wrong at x=" + x);
                }
                if (L.Aid == CrossingAid.Drawbridge && !L.Placements.Any(p => p.TrapId == "AID_SWITCH" && p.Y < L.CanalRow)) e.Add(tag + ": drawbridge without a switch");
            }

            // entrance rows empty
            foreach (var p in L.Placements) if (p.Y <= 1) e.Add(tag + ": placement in the entrance safe zone");
            if (L.PortalTile.HasValue && L.PortalTile.Value.Y <= 1) e.Add(tag + ": portal in the entrance safe zone");
            if (L.RewardTile.HasValue && L.RewardTile.Value.Y <= 1) e.Add(tag + ": reward in the entrance safe zone");
            if (L.PortalTile.HasValue && (L.Room == 1 || L.Room == TowerConfig.RoomsPerFloor)) e.Add(tag + ": portal in room 1/10");
            if (L.PortalTile.HasValue && onPath(L.PortalTile.Value.X, L.PortalTile.Value.Y)) e.Add(tag + ": portal on the golden path");

            // crown altar
            if (L.IsCrown)
                foreach (var p in L.Placements)
                    if (p.Shape == TrapShape.Tile && Math.Abs(p.X - L.Goal.X) <= 1 && Math.Abs(p.Y - L.Goal.Y) <= 1) e.Add(tag + ": trap inside the crown circle");

            // tiles never double-booked
            var cells = new HashSet<long>();
            foreach (var p in L.Placements.Where(p => p.Shape == TrapShape.Tile && p.TrapId != "AID_SWITCH")) if (!cells.Add(p.X * 1000L + p.Y)) e.Add(tag + ": two traps on one tile");
            if (L.RewardTile.HasValue && cells.Contains(L.RewardTile.Value.X * 1000L + L.RewardTile.Value.Y)) e.Add(tag + ": reward on a trap tile");
            if (L.RewardTile.HasValue && L.Tiles[L.RewardTile.Value.X, L.RewardTile.Value.Y] == TileKind.Hazard) e.Add(tag + ": reward on a hazard");

            // mutator rules
            if (L.MutatorId != null)
            {
                var m = cat.Mutators.FirstOrDefault(x => x.Id == L.MutatorId);
                if (m == null) e.Add(tag + ": unknown mutator");
                else
                {
                    if (L.Room == 1) e.Add(tag + ": mutator in room 1");
                    if (L.IsGauntlet && !m.GauntletSafe) e.Add(tag + ": non-gauntlet-safe mutator in room 10");
                    foreach (var p in L.Placements) if (m.Conflicts.Contains(p.TrapId)) e.Add(tag + ": mutator " + m.Id + " conflicts with " + p.TrapId);
                    if (L.RewardId != null && m.Conflicts.Contains(L.RewardId)) e.Add(tag + ": mutator conflicts with reward");
                }
            }
            return e;
        }
    }

    public static class RoomMetrics
    {
        /// <summary>Rough no-hit traversal estimate (6 m/s, 2 m tiles). Timed lanes cost the expected wait at a random phase.</summary>
        public static double EstimateTraversalSeconds(RoomLayout L, Catalog cat)
        {
            double t = (L.GoldenPath.Count - 1) * 2.0 / 6.0;
            foreach (var p in L.Placements)
            {
                if (p.TrapId == "AID_SWITCH") { t += 0.5; continue; }
                var d = cat.Traps.First(x => x.Id == p.TrapId);
                if (d.Policy == PathPolicy.TimedBlocks && p.OnGoldenPath)
                {
                    // arrival at a random phase: wait unless the open window left is long enough to cross the whole gate.
                    // E[wait] = (blocked + need)^2 / (2 * cycle), need = 0.14 s margin + 0.333 s per tile crossed
                    int run = 1;
                    if (p.Shape == TrapShape.FullRow) run = Math.Max(1, L.GoldenPath.Count(c => c.Y == p.Y));
                    double need = 0.14 + run * (RoomSpace.Tile / 6.0);
                    double cycle = d.SafeWindow + TrapClock.BlockedSeconds;
                    t += (TrapClock.BlockedSeconds + need) * (TrapClock.BlockedSeconds + need) / (2.0 * cycle);
                }
            }
            if (L.CanalRow >= 0) t += L.Aid == CrossingAid.Drawbridge ? 0.6 : L.Aid == CrossingAid.JumpPad ? 0.3 : 0.4;
            return t;
        }
    }
}
