using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Core;

namespace TowerOfChaos.Rooms
{
    public enum RoomTraversalState
    {
        Untouched = 0, // First player has NOT claimed the room reward yet; 100% environmental traps active
        Traversed = 1  // First player already passed through; one-shot traps spent, player-planted traps active
    }

    public enum FirstRunnerRewardType
    {
        Heal25Percent,
        TemporaryShield50,
        HighTierLootItem,
        SkillCooldownReset
    }

    [Serializable]
    public class EnvironmentalTrapNode
    {
        public string TrapId;
        public bool IsOneShot;
        public bool IsHallucinationTrap;
        public bool IsActive = true;
        public int RemainingCharges = 3; // Configured for 5-player chaos

        public void TriggerOnPlayer(Guid playerId, out bool shouldDeactivate)
        {
            if (!IsActive)
            {
                shouldDeactivate = false;
                return;
            }

            if (IsOneShot)
            {
                IsActive = false;
                RemainingCharges = 0;
                shouldDeactivate = true;
            }
            else
            {
                RemainingCharges = Mathf.Max(0, RemainingCharges - 1);
                if (RemainingCharges == 0)
                {
                    IsActive = false;
                }
                shouldDeactivate = !IsActive;
            }
        }

        public void DisarmManually()
        {
            IsActive = false;
            RemainingCharges = 0;
        }
    }

    [Serializable]
    public class PlayerPlantedTrapInstance
    {
        public Guid InstanceId = Guid.NewGuid();
        public Guid OwnerPlayerId;
        public string TacticalItemId;
        public Vector3 LocalPosition;
        public bool IsTriggered;
    }

    /// <summary>
    /// Controls a single 3D Room inside the 10x10 Tower.
    /// Handles First-Runner exclusive rewards, PvP 5-second door lockdown,
    /// Starter Chests in Floor 1 (Rooms 1-5), and player-planted traps.
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("Room Coordinates (1-Based)")]
        [Range(1, GameConstants.TotalFloors)] public int FloorNumber = 1;
        [Range(1, GameConstants.RoomsPerFloor)] public int RoomNumber = 1;

        [Header("State & Biome")]
        public FloorBiome Biome;
        public RoomTraversalState State = RoomTraversalState.Untouched;

        [Header("First-Runner & Starter Balance Rewards")]
        public bool HasFirstRunnerReward = true;
        public FirstRunnerRewardType RewardType;
        public bool HasFloorOneStarterChest; // True ONLY for Floor 1, Rooms 1..5 to balance low Opening Dice rolls!
        public bool IsStarterChestClaimed;

        [Header("Doors & PvP Lockdown")]
        public bool IsExitDoorLocked;
        private float _pvpLockRemainingSec;
        private bool _hasTriggeredPvPLockThisEncounter;

        [Header("Traps & Drops")]
        public List<EnvironmentalTrapNode> EnvironmentalTraps = new List<EnvironmentalTrapNode>();
        public List<PlayerPlantedTrapInstance> PlayerPlantedTraps = new List<PlayerPlantedTrapInstance>();
        public List<string> DroppedLootItemsOnFloor = new List<string>();

        private readonly HashSet<Guid> _occupyingPlayers = new HashSet<Guid>();

        public int GlobalRoomNumber => ((FloorNumber - 1) * GameConstants.RoomsPerFloor) + RoomNumber;
        public bool IsFinalCrownRoom => FloorNumber == GameConstants.TotalFloors && RoomNumber == GameConstants.RoomsPerFloor;
        public bool IsFloorAscensionRoom => RoomNumber == GameConstants.RoomsPerFloor && FloorNumber < GameConstants.TotalFloors;
        public int ActivePlayerCount => _occupyingPlayers.Count;

        public event Action<int, int, Guid, FirstRunnerRewardType> OnFirstRunnerRewardClaimed;
        public event Action<int, int, bool> OnDoorLockStateChanged;

        public void InitializeRoom(int floor, int room, int seed)
        {
            FloorNumber = floor;
            RoomNumber = room;
            Biome = (FloorBiome)((floor - 1) / 2);
            State = RoomTraversalState.Untouched;

            // Floor 1 Rooms 1..5 receive guaranteed Starter Loot Chests to balance Opening Dice 1..5 vs 6
            HasFloorOneStarterChest = (floor == 1 && room <= GameConstants.GuaranteedStarterLootMaxRoom);
            IsStarterChestClaimed = false;
            HasFirstRunnerReward = true;

            var rng = new System.Random(seed + GlobalRoomNumber * 97);
            RewardType = (FirstRunnerRewardType)rng.Next(0, 4);
        }

        private void Update()
        {
            if (IsExitDoorLocked && _pvpLockRemainingSec > 0f)
            {
                _pvpLockRemainingSec -= Time.deltaTime;
                if (_pvpLockRemainingSec <= 0f)
                {
                    UnlockExitDoor();
                }
            }
        }

        /// <summary>
        /// Called by the authoritative server when a player enters this room.
        /// </summary>
        public void OnPlayerEnteredRoom(Guid playerId)
        {
            _occupyingPlayers.Add(playerId);

            // If 2 or more players are simultaneously in the room, trigger the 5-second PvP Energy Lock!
            if (_occupyingPlayers.Count >= 2 && !_hasTriggeredPvPLockThisEncounter)
            {
                _hasTriggeredPvPLockThisEncounter = true;
                LockExitDoor(GameConstants.PvPRoomLockDurationSec);
            }
        }

        /// <summary>
        /// Called when a player leaves or dies in this room.
        /// </summary>
        public void OnPlayerExitedOrDied(Guid playerId)
        {
            _occupyingPlayers.Remove(playerId);

            // If only 1 player remains during a PvP lock (e.g. rival died), immediately open the door
            if (_occupyingPlayers.Count < 2 && IsExitDoorLocked)
            {
                UnlockExitDoor();
            }

            if (_occupyingPlayers.Count == 0)
            {
                _hasTriggeredPvPLockThisEncounter = false;
            }
        }

        /// <summary>
        /// Claims the exclusive First-Runner reward if this player is the first to touch the orb/chest.
        /// Transitions the room to Traversed state once cleared.
        /// </summary>
        public bool TryClaimFirstRunnerReward(Guid playerId, out FirstRunnerRewardType grantedReward)
        {
            if (State == RoomTraversalState.Untouched && HasFirstRunnerReward)
            {
                HasFirstRunnerReward = false;
                State = RoomTraversalState.Traversed;
                grantedReward = RewardType;
                OnFirstRunnerRewardClaimed?.Invoke(FloorNumber, RoomNumber, playerId, grantedReward);
                return true;
            }

            grantedReward = default;
            return false;
        }

        /// <summary>
        /// Claims the Floor 1 (Rooms 1-5) Starter Chest designed to arm players who rolled low on the Opening Dice.
        /// </summary>
        public bool TryClaimFloorOneStarterChest(Guid playerId, out string grantedTacticalItemId, out float bonusShield)
        {
            if (HasFloorOneStarterChest && !IsStarterChestClaimed)
            {
                IsStarterChestClaimed = true;
                grantedTacticalItemId = (RoomNumber % 2 == 1) ? "item_cryo_grenade" : "item_grappling_hook";
                bonusShield = 25f;
                return true;
            }

            grantedTacticalItemId = null;
            bonusShield = 0f;
            return false;
        }

        /// <summary>
        /// Allows the first/earlier runner to plant a trap from their backpack for chasers behind them.
        /// </summary>
        public void PlantPlayerTrap(Guid ownerId, string tacticalItemId, Vector3 localPos)
        {
            PlayerPlantedTraps.Add(new PlayerPlantedTrapInstance
            {
                OwnerPlayerId = ownerId,
                TacticalItemId = tacticalItemId,
                LocalPosition = localPos,
                IsTriggered = false
            });
        }

        /// <summary>
        /// Called when a player dies in this room: drops 1 looted item on the floor for killer/chasers.
        /// </summary>
        public void SpawnDroppedLootOnFloor(string lootItemId)
        {
            if (!string.IsNullOrEmpty(lootItemId))
            {
                DroppedLootItemsOnFloor.Add(lootItemId);
            }
        }

        private void LockExitDoor(float durationSec)
        {
            IsExitDoorLocked = true;
            _pvpLockRemainingSec = durationSec;
            OnDoorLockStateChanged?.Invoke(FloorNumber, RoomNumber, true);
        }

        private void UnlockExitDoor()
        {
            IsExitDoorLocked = false;
            _pvpLockRemainingSec = 0f;
            OnDoorLockStateChanged?.Invoke(FloorNumber, RoomNumber, false);
        }
    }
}
