using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerOfChaos.GameCore
{
    /// <summary>Random = standalone 25% roll; Force/None are used by <see cref="MatchWorld"/> to enforce the per-floor cap.</summary>
    public enum MutatorMode { Random, Force, None }

    public sealed class RoomRequest
    {
        public MutatorMode Mutator = MutatorMode.Random;
        public int Floor, Room;
        public long MatchSeed;
        public int ActivePlayers = 5;     // k, SPEC 15.3
        public int PlayersInRoom = 2;     // n, SPEC 15.3
        public bool HasPortal;            // chosen by the world plan (never rooms 1 / 10)
        public bool ComebackUpgrade;      // SPEC 15.4 - reward rarity one tier higher
    }

    /// <summary>
    /// Deterministic room generator with the SPEC 6 anti-softlock guarantees:
    /// golden walkable path, crossing aids over full-width hazards, >=1.2 s timed windows on the path,
    /// trap-free entrance, TBP budget, tier-C caps, 45% occupancy cap. Output is re-checked by <see cref="RoomValidator"/>.
    /// </summary>
    public sealed class SolvableRoomGenerator
    {
        private readonly TowerConfig _cfg;
        private readonly Catalog _cat;
        private readonly List<TrapDef> _traps;

        public SolvableRoomGenerator(TowerConfig cfg, Catalog catalog)
        {
            _cfg = cfg;
            _cat = catalog;
            _traps = catalog.Traps.Where(t => t.HomeFloor <= cfg.Height).ToList();
        }

        public RoomLayout Generate(RoomRequest q)
        {
            if (q.Floor < 1 || q.Floor > _cfg.Height) throw new ArgumentOutOfRangeException("Floor");
            if (q.Room < 1 || q.Room > TowerConfig.RoomsPerFloor) throw new ArgumentOutOfRangeException("Room");
            if (q.HasPortal && (q.Room == 1 || q.Room == TowerConfig.RoomsPerFloor))
                throw new ArgumentException("Portals are never in room 1 or room 10 (SPEC 9)");

            var rng = DeterministicRng.For(q.MatchSeed, q.Floor, q.Room, 1);
            bool gauntlet = TowerConfig.IsGauntletRoom(q.Room);
            bool crown = _cfg.IsCrownRoom(q.Floor, q.Room);
            int cols = _cfg.Cols(q.Room), rows = _cfg.Rows(q.Room), mid = cols / 2;

            var L = new RoomLayout
            {
                Floor = q.Floor, Room = q.Room, Cols = cols, Rows = rows, IsGauntlet = gauntlet, IsCrown = crown,
                Tiles = new TileKind[cols, rows],
                Start = new Cell(mid, 0),
                Goal = crown ? new Cell(mid, rows / 2) : new Cell(mid, rows - 1),
                ZoneStartRow = 2,
                ZoneEndRow = crown ? rows - 2 : gauntlet ? rows - 4 : rows - 2,
                Budget = TowerRules.TrapBudget(q.Floor, q.Room, q.ActivePlayers)
            };
            var used = new bool[cols, rows];
            var blocked = new bool[cols, rows];
            for (int x = 0; x < cols; x++) { L.Tiles[x, 0] = TileKind.EntranceSafe; L.Tiles[x, 1] = TileKind.EntranceSafe; blocked[x, 0] = blocked[x, 1] = true; }
            for (int y = L.ZoneEndRow + 1; y < rows; y++) for (int x = 0; x < cols; x++) blocked[x, y] = true;
            if (crown)
            {
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) blocked[L.Goal.X + dx, L.Goal.Y + dy] = true;
            }

            // ---- 1. golden path -------------------------------------------------------------
            var onPath = new bool[cols, rows];
            Action<int, int> addPath = (px, py) =>
            {
                if (onPath[px, py]) return;
                onPath[px, py] = true;
                L.GoldenPath.Add(new Cell(px, py));
                if (py >= L.ZoneStartRow) L.Tiles[px, py] = TileKind.GoldenPath;
            };
            addPath(mid, 0); addPath(mid, 1);
            int cx = mid;
            for (int y = 2; y <= L.Goal.Y; y++)
            {
                int remaining = L.Goal.Y - y;
                var cand = new List<int>(); var w = new List<int>();
                for (int nx = Math.Max(1, cx - 2); nx <= Math.Min(cols - 2, cx + 2); nx++)
                {
                    if (Math.Abs(nx - L.Goal.X) > remaining * 2) continue;
                    if (remaining == 0 && nx != L.Goal.X) continue;
                    cand.Add(nx); w.Add(nx == cx ? 3 : 1);
                }
                int nextX = cand[WeightedIndex(w, rng)];
                addPath(cx, y);
                int step = nextX > cx ? 1 : -1;
                for (int px = cx; px != nextX;) { px += step; addPath(px, y); }
                cx = nextX;
            }
            if (crown) L.Tiles[L.Goal.X, L.Goal.Y] = TileKind.Altar;

            // timed gates on the path: capped, and always separated by >= 1 safe path tile
            var pathIdx = new Dictionary<long, int>();
            for (int i = 0; i < L.GoldenPath.Count; i++) pathIdx[L.GoldenPath[i].X * 1000L + L.GoldenPath[i].Y] = i;
            var timedCovered = new HashSet<int>();
            int gates = 0, gateCap = TowerRules.MaxTimedGatesPerRoom(q.Floor);

            // ---- 2. mutator (chosen first so trap/reward pools can honour conflicts) ----------
            MutatorDef mut = PickMutator(q, gauntlet, rng);
            L.MutatorId = mut == null ? null : mut.Id;
            var banned = new HashSet<string>(mut == null ? new string[0] : mut.Conflicts);

            // ---- 3. guardian / monster -------------------------------------------------------
            if (crown) { L.MonsterLock = true; L.MonsterSpawn = new Cell(L.Goal.X + 2, L.Goal.Y); used[L.Goal.X + 2, L.Goal.Y] = true; }
            else if (gauntlet) { L.MonsterLock = true; L.MonsterSpawn = new Cell(mid, rows - 3); }
            else if (q.Room >= 2 && rng.NextDouble() < 0.35)
            {
                L.MonsterLock = true;
                var c = FreeCell(L, used, blocked, onPath, rng, L.ZoneStartRow + 2, L.ZoneEndRow, false);
                if (c.HasValue) { L.MonsterSpawn = c; used[c.Value.X, c.Value.Y] = true; }
                else L.MonsterLock = false;
            }

            // ---- 4. optional full-width hazard canal with a crossing aid ---------------------
            int remainingBudget = L.Budget;
            int zoneTiles = cols * (L.ZoneEndRow - L.ZoneStartRow + 1);
            int occupancyCap = (int)Math.Floor(zoneTiles * TowerRules.TrapZoneMaxOccupancy);
            int occupied = 0, tierC = 0;
            var canalDefs = _traps.Where(t => t.Policy == PathPolicy.BlocksWithBypass && (t.Id == "T31" || t.Id == "T32") && !banned.Contains(t.Id)).ToList();
            if (q.Floor >= 3 && !gauntlet && canalDefs.Count > 0 && rng.NextDouble() < 0.30)
            {
                var def = canalDefs[rng.Next(canalDefs.Count)];
                int hi = L.ZoneEndRow - 1;
                var canalRows = new List<int>();
                for (int r = 3; r <= hi; r++) if (!RowHasAny(used, cols, r)) canalRows.Add(r);
                if (def.Cost <= remainingBudget && canalRows.Count > 0 && cols <= occupancyCap - occupied)
                {
                    int row = canalRows[rng.Next(canalRows.Count)];
                    L.CanalRow = row;
                    L.Aid = (CrossingAid)rng.Next(1, 4);
                    for (int x = 0; x < cols; x++)
                    {
                        L.Tiles[x, row] = onPath[x, row] ? TileKind.CrossingAid : TileKind.Hazard;
                        used[x, row] = true;
                    }
                    L.Placements.Add(new TrapPlacement { TrapId = def.Id, X = 0, Y = row, Shape = TrapShape.FullRow, OnGoldenPath = false });
                    if (L.Aid == CrossingAid.Drawbridge)
                    {
                        var sw = L.GoldenPath.First(c => c.Y == row - 1);
                        used[sw.X, sw.Y] = true;
                        L.Placements.Add(new TrapPlacement { TrapId = "AID_SWITCH", X = sw.X, Y = sw.Y, Shape = TrapShape.Tile, OnGoldenPath = true });
                    }
                    remainingBudget -= def.Cost; occupied += cols; L.Spent += def.Cost;
                    if (def.Tier == CostTier.C) tierC++;
                }
            }

            // ---- 5. fill with traps until the TBP budget is spent ------------------------------
            var pool = _traps.Where(t => !banned.Contains(t.Id)).ToList();
            int tierCCap = TowerRules.MaxTierCPerRoom(q.Floor);
            for (int attempt = 0; attempt < 200 && remainingBudget > 0; attempt++)
            {
                var cands = ChoosePool(pool, q.Floor, rng);
                var ok = cands.Where(t => t.Cost <= remainingBudget && (t.Tier != CostTier.C || tierC < tierCCap)).ToList();
                if (ok.Count == 0) { ok = pool.Where(t => t.Cost <= remainingBudget && (t.Tier != CostTier.C || tierC < tierCCap)).ToList(); if (ok.Count == 0) break; }
                var weights = ok.Select(t => t.Interactive ? (int)Math.Round(TowerRules.InteractiveWeight(q.PlayersInRoom) * 4) : 4).ToList();
                var def = ok[WeightedIndex(weights, rng)];

                if (def.Shape == TrapShape.FullRow)
                {
                    if (def.Policy != PathPolicy.TimedBlocks || def.SafeWindow < TowerRules.MinSafeWindowSeconds) continue;
                    if (occupied + cols > occupancyCap || gates >= gateCap) continue;
                    var rowsOk = new List<int>();
                    for (int y = L.ZoneStartRow; y <= L.ZoneEndRow; y++)
                    {
                        if (RowHasAny(used, cols, y) || RowHasFullRow(L, y - 1) || RowHasFullRow(L, y + 1) || y == L.CanalRow - 1 || y == L.CanalRow + 1) continue;
                        bool free = true; for (int x = 0; x < cols; x++) if (blocked[x, y]) free = false;
                        if (!free) continue;
                        int lo = int.MaxValue, hi2 = int.MinValue;
                        for (int i = 0; i < L.GoldenPath.Count; i++) if (L.GoldenPath[i].Y == y) { lo = Math.Min(lo, i); hi2 = Math.Max(hi2, i); }
                        if (lo == int.MaxValue) continue;
                        bool clear = true; for (int i = lo - 1; i <= hi2 + 1; i++) if (timedCovered.Contains(i)) clear = false;
                        if (clear) rowsOk.Add(y);
                    }
                    if (rowsOk.Count == 0) continue;
                    int row = rowsOk[rng.Next(rowsOk.Count)];
                    for (int i = 0; i < L.GoldenPath.Count; i++) if (L.GoldenPath[i].Y == row) timedCovered.Add(i);
                    gates++;
                    for (int x = 0; x < cols; x++) { used[x, row] = true; if (!onPath[x, row]) L.Tiles[x, row] = TileKind.Trap; }
                    L.Placements.Add(new TrapPlacement { TrapId = def.Id, X = 0, Y = row, Shape = TrapShape.FullRow, OnGoldenPath = true });
                    occupied += cols;
                }
                else
                {
                    if (occupied + 1 > occupancyCap) continue;
                    var cell = FreeCell(L, used, blocked, onPath, rng, L.ZoneStartRow, L.ZoneEndRow, true);
                    if (!cell.HasValue) continue;
                    var c = cell.Value;
                    bool path = onPath[c.X, c.Y];
                    if (path && !TowerRules.IsAllowedOnGoldenPath(def)) continue;
                    if (path && L.Tiles[c.X, c.Y] == TileKind.CrossingAid) continue;
                    if (path && def.Policy == PathPolicy.TimedBlocks)
                    {
                        int pi = pathIdx[c.X * 1000L + c.Y];
                        if (gates >= gateCap || timedCovered.Contains(pi - 1) || timedCovered.Contains(pi) || timedCovered.Contains(pi + 1)) continue;
                        timedCovered.Add(pi); gates++;
                    }
                    used[c.X, c.Y] = true;
                    if (!path) L.Tiles[c.X, c.Y] = def.Policy == PathPolicy.BlocksWithBypass ? TileKind.Hazard : TileKind.Trap;
                    L.Placements.Add(new TrapPlacement { TrapId = def.Id, X = c.X, Y = c.Y, Shape = TrapShape.Tile, OnGoldenPath = path });
                    occupied++;
                }
                remainingBudget -= def.Cost; L.Spent += def.Cost;
                if (def.Tier == CostTier.C) tierC++;
            }

            // ---- 6. portal, reward orb ---------------------------------------------------------
            if (q.HasPortal)
            {
                var pc = FreeCell(L, used, blocked, onPath, rng, L.ZoneStartRow, L.ZoneEndRow, false, requireNearPath: true)
                         ?? FreeCell(L, used, blocked, onPath, rng, L.ZoneStartRow, L.ZoneEndRow, false);
                if (!pc.HasValue) throw new InvalidOperationException("No free tile for portal");
                L.PortalTile = pc; used[pc.Value.X, pc.Value.Y] = true; L.Tiles[pc.Value.X, pc.Value.Y] = TileKind.Portal;
            }
            if (!crown)
            {
                var pathFree = L.GoldenPath.Where(c => c.Y >= L.ZoneStartRow && c.Y <= L.ZoneEndRow && !used[c.X, c.Y]).ToList();
                Cell? orb = null;
                if (pathFree.Count > 0) orb = pathFree[pathFree.Count / 2];
                else orb = FreeCell(L, used, blocked, onPath, rng, L.ZoneStartRow, L.ZoneEndRow, false, requireNearPath: true);
                if (orb.HasValue)
                {
                    L.RewardTile = orb; used[orb.Value.X, orb.Value.Y] = true;
                    L.RewardId = PickReward(q, banned, rng);
                }
            }
            return L;
        }

        // ------------------------------------------------------------------------------ helpers
        private static int WeightedIndex(List<int> w, DeterministicRng rng)
        {
            int total = 0; foreach (var x in w) total += Math.Max(0, x);
            if (total <= 0) return rng.Next(w.Count);
            int r = rng.Next(total);
            for (int i = 0; i < w.Count; i++) { r -= Math.Max(0, w[i]); if (r < 0) return i; }
            return w.Count - 1;
        }

        private static bool RowHasAny(bool[,] used, int cols, int y) { for (int x = 0; x < cols; x++) if (used[x, y]) return true; return false; }

        private static bool RowHasFullRow(RoomLayout L, int y) { return L.Placements.Any(p => p.Shape == TrapShape.FullRow && p.Y == y); }

        private static Cell? FreeCell(RoomLayout L, bool[,] used, bool[,] blocked, bool[,] onPath, DeterministicRng rng,
                                      int y0, int y1, bool allowOnPath, bool requireNearPath = false)
        {
            var list = new List<Cell>();
            for (int y = y0; y <= y1; y++)
                for (int x = 0; x < L.Cols; x++)
                {
                    if (used[x, y] || blocked[x, y]) continue;
                    if (onPath[x, y] && !allowOnPath) continue;
                    if (L.Tiles[x, y] == TileKind.CrossingAid || L.Tiles[x, y] == TileKind.Hazard) continue;
                    if (requireNearPath)
                    {
                        bool near = (x > 0 && onPath[x - 1, y]) || (x < L.Cols - 1 && onPath[x + 1, y]) || (y > 0 && onPath[x, y - 1]) || (y < L.Rows - 1 && onPath[x, y + 1]);
                        if (!near) continue;
                    }
                    list.Add(new Cell(x, y));
                }
            if (list.Count == 0) return null;
            return list[rng.Next(list.Count)];
        }

        /// <summary>SPEC 15.2: 70% home genre, 10% universal, 20% chaos; floor 10 mixes everything.</summary>
        private List<TrapDef> ChoosePool(List<TrapDef> pool, int floor, DeterministicRng rng)
        {
            if (floor == 10) return pool;
            double r = rng.NextDouble();
            List<TrapDef> sel;
            if (r < 0.70) sel = pool.Where(t => t.HomeFloor == floor || (floor == 1 && t.HomeFloor == 0)).ToList();
            else if (r < 0.80) sel = pool.Where(t => t.HomeFloor == 0).ToList();
            else sel = pool.Where(t => t.HomeFloor != floor && t.HomeFloor != 0).ToList();
            return sel.Count > 0 ? sel : pool;
        }

        private MutatorDef PickMutator(RoomRequest q, bool gauntlet, DeterministicRng rng)
        {
            if (q.Room == 1 || q.Floor < 2 || q.Mutator == MutatorMode.None) return null;
            if (q.Mutator == MutatorMode.Random && rng.NextDouble() >= 0.25) return null;
            var elig = _cat.Mutators.Where(m => m.Floors.Contains(q.Floor) && (m.Release == "v1.0" || _cfg.Height > 6) && (!gauntlet || m.GauntletSafe)).ToList();
            return elig.Count == 0 ? null : elig[rng.Next(elig.Count)];
        }

        private string PickReward(RoomRequest q, HashSet<string> banned, DeterministicRng rng)
        {
            var avail = _cat.Rewards.Where(r => !banned.Contains(r.Id) && (r.Release == "v1.0" || _cfg.Height > 6) &&
                                                (r.Rarity != Rarity.Legendary || q.Floor <= 8)).ToList();
            double roll = rng.NextDouble() * 100.0;
            bool legendaryOk = avail.Any(r => r.Rarity == Rarity.Legendary);
            Rarity rar = roll < 60 ? Rarity.Common : roll < 88 ? Rarity.Rare : (roll < 98 || !legendaryOk) ? Rarity.Epic : Rarity.Legendary;
            if (q.ComebackUpgrade && rar < Rarity.Legendary) rar = (Rarity)((int)rar + 1);
            var list = avail.Where(r => r.Rarity == rar).ToList();
            if (list.Count == 0) list = avail.Where(r => r.Rarity == Rarity.Common).ToList();
            return list[rng.Next(list.Count)].Id;
        }
    }
}
