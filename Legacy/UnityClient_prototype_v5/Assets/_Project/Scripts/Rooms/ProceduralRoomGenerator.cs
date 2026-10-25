using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Core;

namespace TowerOfChaos.Rooms
{
    public enum TrapCategory
    {
        MindAndHallucination = 0, // T01-T07: Inverted Gas, Blindness, Mirage Door, Mimic, Phantom, Mirror Flip, Swapped Reality
        ElementalAndTerrain = 1,  // T08-T13: Whirlpool Water, Acid Pool, Ice Floor, Gale Wind, Conveyor, Lightning
        KineticAndMechanical = 2, // T14-T19: Pendulum, Cracked Tile, Pinball Bumper, Rhythmic Spikes, Crusher, Boulder
        GreedAndTemptation = 3,   // T20-T22: Greed Cage, Blood Altar, Roulette Pedestal
        MultiplayerInteractive = 4 // T23-T25: Betrayal Lever, Seesaw Bridge, Hot Potato Orb
    }

    public enum CrossingAidType
    {
        None = 0,
        MovingSteppingStones = 1, // Lily pads / moving stone platforms over water or acid
        FreeEnvironmentJumpPad = 2, // Brawl-Stars style spring pad that launches any player across the hazard
        PressureDrawbridge = 3    // Switch before the hazard that extends a bridge for 5 seconds
    }

    public enum TileCellState
    {
        EmptyWalkable = 0,
        EntranceSafeZone = 1,     // Rows 0-1: 100% trap-free so players can see the room on entry
        GoldenWalkablePath = 2,   // Guaranteed solvable path; ONLY rhythmic/timed or hallucination traps allowed!
        StaticImpassableHazard = 3, // Deep water, acid, permanent spikes (NEVER allowed on Golden Path without a Crossing Aid)
        CrossingAidTile = 4,      // Bridge, Stepping Stone, or Free Jump Pad guaranteeing traversal without jump skill
        PortalGateTile = 5,
        FirstRunnerRewardTile = 6
    }

    /// <summary>
    /// Extensible ScriptableObject definition for Traps (Live-Ops Update Ready).
    /// Designers can add new traps in future updates simply by creating a new asset in the catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrapDefinition", menuName = "TowerOfChaos/Modular Trap Definition")]
    public class ScriptableTrapDefinition : ScriptableObject
    {
        public string TrapId;
        public string NameFA;
        public string NameEN;
        public TrapCategory Category;
        public FloorBiome[] AllowedBiomes;
        public int WidthTiles = 1;
        public int LengthTiles = 1;
        public bool IsStaticImpassableHazard; // True for Deep Water, Acid Pool, Permanent Spike Wall
        public bool HasSafeTimingWindow = true; // Must have >= 1.2s safe window if placed on Golden Path
        public float SafeWindowSeconds = 1.5f;
        public float BaseDamage = 25f;
        public int ChargesIn5PlayerMode = 3;
        public GameObject TrapPrefab;
    }

    /// <summary>
    /// Extensible ScriptableObject definition for First-Runner Room Rewards (Live-Ops Update Ready).
    /// </summary>
    [CreateAssetMenu(fileName = "NewRewardDefinition", menuName = "TowerOfChaos/Modular Reward Definition")]
    public class ScriptableRewardDefinition : ScriptableObject
    {
        public string RewardId;
        public string NameFA;
        public string NameEN;
        [Range(1f, 100f)] public float SpawnWeight = 10f;
        public SpriteHudIcon Icon;
        public float HealAmount;
        public float ShieldAmount;
        public float SpeedBuffMultiplier = 1f;
        public float BuffDurationSec;
        public bool GrantsWaterAndSpikeWalk; // Ghostwalker Boots
        public bool GrantsFreeOracleScanToken;
        public bool CursesCurrentMatchLeader; // Leader's Curse comeback mechanic
        public string GrantedLootItemId;

        [Serializable]
        public struct SpriteHudIcon
        {
            public Sprite SpriteAsset;
            public Color TintColor;
        }
    }

    [Serializable]
    public struct SpawnedRoomTrapPlacement
    {
        public string TrapId;
        public int GridX;
        public int GridY;
        public CrossingAidType AttachedCrossingAid;
    }

    /// <summary>
    /// Procedural 7x10 Tile Grid Room Generator with 100% Mathematical Solvability Guarantee.
    /// Ensures NO room can ever spawn an impassable wall of spikes or deep water without a walkable path,
    /// stepping stones, or a free Brawl-Stars style Jump Pad.
    /// </summary>
    public class ProceduralRoomGenerator : MonoBehaviour
    {
        public const int GridColumns = 7; // 7 tiles * 2m = 14m width
        public const int GridRows = 10;   // 10 tiles * 2m = 20m length
        public const int DoorColumn = 3;  // Center column (0..6)

        [Header("Extensible Live-Ops Catalogs")]
        public List<ScriptableTrapDefinition> RegisteredTrapCatalog = new List<ScriptableTrapDefinition>();
        public List<ScriptableRewardDefinition> RegisteredRewardCatalog = new List<ScriptableRewardDefinition>();

        /// <summary>
        /// Generates a randomized room layout for (Floor, Room) using the authoritative match seed,
        /// while strictly guaranteeing a valid walkable path from Entrance [3,0] to Exit [3,9].
        /// </summary>
        public TileCellState[,] GenerateSolvableRoomLayout(
            int floor,
            int room,
            int matchSeed,
            bool hasPortalInRoom,
            out List<SpawnedRoomTrapPlacement> trapPlacements,
            out string selectedFirstRunnerRewardId)
        {
            trapPlacements = new List<SpawnedRoomTrapPlacement>();
            var grid = new TileCellState[GridColumns, GridRows];
            var rng = new System.Random(matchSeed ^ (floor * 1009 + room * 9173));

            // 1. Lock Entrance Safe Zone (Row 0 and Row 1 around the door)
            for (int x = 0; x < GridColumns; x++)
            {
                grid[x, 0] = TileCellState.EntranceSafeZone;
            }
            grid[DoorColumn, 1] = TileCellState.EntranceSafeZone;

            // 2. Carve the "Golden Solvable Walkable Path" from Entrance [3,1] to Exit [3,9]
            int currentX = DoorColumn;
            for (int y = 1; y < GridRows; y++)
            {
                grid[currentX, y] = TileCellState.GoldenWalkablePath;

                // Winding path towards the exit door at [DoorColumn, GridRows - 1]
                if (y < GridRows - 2)
                {
                    int stepX = rng.Next(-1, 2); // -1, 0, or +1
                    int nextX = Mathf.Clamp(currentX + stepX, 1, GridColumns - 2);
                    grid[nextX, y] = TileCellState.GoldenWalkablePath;
                    currentX = nextX;
                }
                else
                {
                    // Converge back to DoorColumn at the top of the room
                    while (currentX != DoorColumn)
                    {
                        currentX += (DoorColumn > currentX) ? 1 : -1;
                        grid[currentX, y] = TileCellState.GoldenWalkablePath;
                    }
                }
            }

            // 3. Place First-Runner Reward Orb along the Golden Path near the middle of the room
            grid[DoorColumn, 5] = TileCellState.FirstRunnerRewardTile;
            selectedFirstRunnerRewardId = SelectWeightedRewardId(rng);

            // 4. Optional Full-Width Elemental Hazard (Water Canal / Acid Pool in Row 4 or 6)
            // RULE: If a water/acid canal spans the room, it MUST spawn a Crossing Aid on the Golden Path!
            if (floor >= 3 && rng.NextDouble() < 0.45)
            {
                int hazardRow = (rng.Next(0, 2) == 0) ? 4 : 6;
                CrossingAidType aid = (CrossingAidType)rng.Next(1, 4); // MovingStones, FreeJumpPad, or Drawbridge

                for (int x = 0; x < GridColumns; x++)
                {
                    if (grid[x, hazardRow] == TileCellState.GoldenWalkablePath)
                    {
                        // Place the guaranteed Crossing Aid right on the Golden Path!
                        grid[x, hazardRow] = TileCellState.CrossingAidTile;
                        trapPlacements.Add(new SpawnedRoomTrapPlacement
                        {
                            TrapId = "T08_WhirlpoolCanal_WithAid",
                            GridX = x,
                            GridY = hazardRow,
                            AttachedCrossingAid = aid
                        });
                    }
                    else
                    {
                        grid[x, hazardRow] = TileCellState.StaticImpassableHazard;
                    }
                }
            }

            // 5. Populate Remaining Tiles with Biome-Appropriate Traps
            int targetTrapCount = 4 + (floor / 2); // Scales excitement up to Floor 10
            for (int i = 0; i < targetTrapCount; i++)
            {
                int tx = rng.Next(0, GridColumns);
                int ty = rng.Next(2, GridRows - 1); // Never in Row 0-1 (Safe Zone) or Exit Row 9

                if (grid[tx, ty] == TileCellState.EmptyWalkable)
                {
                    // Off the golden path: any trap (static or rhythmic) is allowed
                    grid[tx, ty] = TileCellState.StaticImpassableHazard;
                    trapPlacements.Add(new SpawnedRoomTrapPlacement
                    {
                        TrapId = PickRandomTrapIdForFloor(floor, allowStaticImpassable: true, rng),
                        GridX = tx,
                        GridY = ty,
                        AttachedCrossingAid = CrossingAidType.None
                    });
                }
                else if (grid[tx, ty] == TileCellState.GoldenWalkablePath)
                {
                    // ON the golden path: ONLY rhythmic/timed traps (safe window >= 1.2s) or hallucination traps allowed!
                    trapPlacements.Add(new SpawnedRoomTrapPlacement
                    {
                        TrapId = PickRandomTrapIdForFloor(floor, allowStaticImpassable: false, rng),
                        GridX = tx,
                        GridY = ty,
                        AttachedCrossingAid = CrossingAidType.None
                    });
                }
            }

            // 6. Final BFS Pathfinding Solvability Verification (Anti-Softlock Safety Net)
            if (!VerifyPathExistsBfs(grid, new Vector2Int(DoorColumn, 0), new Vector2Int(DoorColumn, GridRows - 1)))
            {
                // Safety fallback: force-clear center column as Golden Walkable Path
                for (int y = 0; y < GridRows; y++)
                {
                    grid[DoorColumn, y] = TileCellState.GoldenWalkablePath;
                }
            }

            return grid;
        }

        /// <summary>
        /// Verifies via Breadth-First Search that a player with ZERO items and ZERO jump skills
        /// can walk from Entrance [3,0] to Exit [3,9].
        /// </summary>
        public static bool VerifyPathExistsBfs(TileCellState[,] grid, Vector2Int start, Vector2Int goal)
        {
            var visited = new bool[GridColumns, GridRows];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { 1, -1, 0, 0 };

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                if (curr == goal) return true;

                for (int dir = 0; dir < 4; dir++)
                {
                    int nx = curr.x + dx[dir];
                    int ny = curr.y + dy[dir];

                    if (nx < 0 || nx >= GridColumns || ny < 0 || ny >= GridRows) continue;
                    if (visited[nx, ny]) continue;

                    // StaticImpassableHazard is the only non-traversable tile type
                    if (grid[nx, ny] != TileCellState.StaticImpassableHazard)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }

            return false;
        }

        private string PickRandomTrapIdForFloor(int floor, bool allowStaticImpassable, System.Random rng)
        {
            // Rhythmic & Hallucination traps that NEVER block pathing permanently
            string[] rhythmicAndMindTraps =
            {
                "T01_InvertedGas",
                "T02_AbyssalBlindness",
                "T05_PhantomRival",
                "T07_SwappedReality",
                "T13_LightningTiles",
                "T14_PendulumScythe",
                "T15_CrackedTile",
                "T17_RhythmicSpikes",
                "T18_CrusherJaws",
                "T25_HotPotatoOrb"
            };

            string[] staticOrBumperTraps =
            {
                "T04_MimicChest",
                "T09_AcidPoolTile",
                "T16_PinballBumper",
                "T20_GreedCage",
                "T21_BloodAltar"
            };

            if (!allowStaticImpassable || rng.NextDouble() < 0.70)
            {
                return rhythmicAndMindTraps[rng.Next(0, rhythmicAndMindTraps.Length)];
            }

            return staticOrBumperTraps[rng.Next(0, staticOrBumperTraps.Length)];
        }

        private string SelectWeightedRewardId(System.Random rng)
        {
            string[] all15Rewards =
            {
                "R01_VitalityOrb_Heal30",
                "R02_TitaniumShield_40",
                "R03_GhostwalkerBoots_WaterSpikeWalk",
                "R04_MiniRocketBoost",
                "R05_ClarityHalo_AntiHallucination",
                "R06_TacticalSupplyBox",
                "R07_SkillOvercharge",
                "R08_ThornMailReflect",
                "R09_AutoSentryOrb",
                "R10_FreeOracleToken",
                "R11_PortalCompass",
                "R12_LeadersCurse",
                "R13_TreasureJackpot",
                "R14_MasterKey_BypassPvPLock",
                "R15_DoubleTrapCharge"
            };

            return all15Rewards[rng.Next(0, all15Rewards.Length)];
        }
    }
}
