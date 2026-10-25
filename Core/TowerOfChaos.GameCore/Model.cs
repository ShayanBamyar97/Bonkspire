using System;
using System.Collections.Generic;

namespace TowerOfChaos.GameCore
{
    public enum CostTier { S = 1, A = 2, B = 3, C = 4 }

    /// <summary>SPEC 18.3.</summary>
    public enum PathPolicy { NeverBlocks, TimedBlocks, BlocksWithBypass }

    public enum TrapShape { Tile, FullRow }

    public enum Rarity { Common, Rare, Epic, Legendary }

    public enum CrossingAid { None, SteppingStones, JumpPad, Drawbridge }

    public enum TileKind
    {
        Empty,
        EntranceSafe,
        GoldenPath,
        Trap,        // passable tile that carries a (non-permanent-blocking) trap
        Hazard,      // permanent obstacle (water / acid / spikes) - impassable without an aid
        CrossingAid, // bridge / stones / jump pad on the golden path
        Portal,
        Altar        // crown altar centre (crown room only)
    }

    public sealed class TrapDef
    {
        public string Id, NameEn, NameFa, Release;
        public int HomeFloor;           // 0 = universal mechanical family
        public CostTier Tier;
        public PathPolicy Policy;
        public TrapShape Shape;
        public double SafeWindow;       // seconds the lane is open (TimedBlocks)
        public bool Interactive;        // multiplayer traps T57..T60
        public int Cost { get { return (int)Tier; } }
    }

    public sealed class RewardDef { public string Id, NameEn, NameFa, Release; public Rarity Rarity; }

    public sealed class MonsterDef { public string Id, NameEn, NameFa, Release; public int Floor; public bool Boss; }

    public sealed class MutatorDef
    {
        public string Id, NameEn, NameFa, Release;
        public int[] Floors;
        public string[] Conflicts;
        public bool GauntletSafe;
    }

    public sealed class Catalog
    {
        public List<TrapDef> Traps = new List<TrapDef>();
        public List<RewardDef> Rewards = new List<RewardDef>();
        public List<MonsterDef> Monsters = new List<MonsterDef>();
        public List<MutatorDef> Mutators = new List<MutatorDef>();

        private Dictionary<string, TrapDef> _trapIndex;
        /// <summary>Lookup by id (index built on first use; the catalog is immutable once loaded).</summary>
        public TrapDef Trap(string id)
        {
            if (_trapIndex == null || _trapIndex.Count != Traps.Count)
            {
                _trapIndex = new Dictionary<string, TrapDef>();
                foreach (var t in Traps) _trapIndex[t.Id] = t;
            }
            return _trapIndex[id];
        }
    }

    /// <summary>Tower geometry and release parameter H (SPEC 29.3). No hard-coded "100" anywhere.</summary>
    public sealed class TowerConfig
    {
        public const int RoomsPerFloor = 10;
        public readonly int Height;

        public TowerConfig(int height)
        {
            if (height < 1 || height > 10) throw new ArgumentOutOfRangeException("height");
            Height = height;
        }

        public int MaxProgress { get { return RoomsPerFloor * Height; } }
        public static int Progress(int floor, int room) { return (floor - 1) * RoomsPerFloor + room; }
        public static bool IsGauntletRoom(int room) { return room == RoomsPerFloor; }
        public bool IsCrownRoom(int floor, int room) { return floor == Height && room == RoomsPerFloor; }
        public int Cols(int room) { return IsGauntletRoom(room) ? 9 : 7; }
        public int Rows(int room) { return IsGauntletRoom(room) ? 10 : 7; }
    }

    public struct Cell
    {
        public int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
    }

    public sealed class TrapPlacement
    {
        public string TrapId;    // or "AID_SWITCH"
        public int X, Y;
        public TrapShape Shape;
        public bool OnGoldenPath;
    }

    public sealed class RoomLayout
    {
        public int Floor, Room, Cols, Rows;
        public bool IsGauntlet, IsCrown;
        public TileKind[,] Tiles;
        public List<Cell> GoldenPath = new List<Cell>();
        public Cell Start, Goal;                      // goal = exit door, or altar for the crown room
        public List<TrapPlacement> Placements = new List<TrapPlacement>();
        public CrossingAid Aid = CrossingAid.None;
        public int CanalRow = -1;
        public Cell? RewardTile;
        public string RewardId;
        public Cell? PortalTile;
        public Cell? MonsterSpawn;
        public bool MonsterLock;
        public string MutatorId;
        public int Budget, Spent;
        public int ZoneStartRow, ZoneEndRow;          // inclusive trap zone
    }
}
