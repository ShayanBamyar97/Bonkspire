using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TowerOfChaos.Rooms;

namespace TowerOfChaos.Core
{
    [Serializable]
    public struct PlayerTowerPosition
    {
        public Guid PlayerId;
        public int Floor; // 1..10
        public int Room;  // 1..10
        public double RoomEntryTimestamp;

        public int GlobalRoomIndex => ((Floor - 1) * GameConstants.RoomsPerFloor) + Room;
    }

    /// <summary>
    /// Manages the 10 Floors x 10 Rooms (100 Rooms) architecture,
    /// GPU Room Streaming/Culling (keeping only [-1, +2] rooms active around the local player for 60 FPS on Android),
    /// and the real-time 5-player vertical Tower Progress Tracker.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        private readonly RoomController[,] _towerGrid = new RoomController[GameConstants.TotalFloors, GameConstants.RoomsPerFloor];
        private readonly Dictionary<Guid, PlayerTowerPosition> _playerPositions = new Dictionary<Guid, PlayerTowerPosition>();

        public event Action<List<PlayerTowerPosition>> OnLeaderboardUpdated;

        /// <summary>
        /// Registers or initializes all 100 rooms of the tower using the authoritative match seed.
        /// </summary>
        public void InitializeTower(RoomController[,] rooms, int matchSeed)
        {
            for (int f = 0; f < GameConstants.TotalFloors; f++)
            {
                for (int r = 0; r < GameConstants.RoomsPerFloor; r++)
                {
                    _towerGrid[f, r] = rooms[f, r];
                    if (_towerGrid[f, r] != null)
                    {
                        _towerGrid[f, r].InitializeRoom(f + 1, r + 1, matchSeed);
                    }
                }
            }
        }

        public RoomController GetRoom(int floor, int room)
        {
            if (floor < 1 || floor > GameConstants.TotalFloors || room < 1 || room > GameConstants.RoomsPerFloor)
            {
                return null;
            }
            return _towerGrid[floor - 1, room - 1];
        }

        /// <summary>
        /// Updates a player's floor/room position on the server and triggers room entry/exit logic.
        /// </summary>
        public void UpdatePlayerRoomPosition(Guid playerId, int newFloor, int newRoom, double serverTimestamp, bool isLocalPlayer = false)
        {
            newFloor = Mathf.Clamp(newFloor, 1, GameConstants.TotalFloors);
            newRoom = Mathf.Clamp(newRoom, 1, GameConstants.RoomsPerFloor);

            if (_playerPositions.TryGetValue(playerId, out var prevPos))
            {
                var prevRoomCtrl = GetRoom(prevPos.Floor, prevPos.Room);
                if (prevRoomCtrl != null && (prevPos.Floor != newFloor || prevPos.Room != newRoom))
                {
                    prevRoomCtrl.OnPlayerExitedOrDied(playerId);
                }
            }

            _playerPositions[playerId] = new PlayerTowerPosition
            {
                PlayerId = playerId,
                Floor = newFloor,
                Room = newRoom,
                RoomEntryTimestamp = serverTimestamp
            };

            var currentRoomCtrl = GetRoom(newFloor, newRoom);
            if (currentRoomCtrl != null)
            {
                currentRoomCtrl.OnPlayerEnteredRoom(playerId);
            }

            if (isLocalPlayer)
            {
                ApplyClientGpuRoomCulling(newFloor, newRoom);
            }

            BroadcastStandings();
        }

        /// <summary>
        /// GPU Optimization for Android 60 FPS:
        /// Only keeps [CurrentGlobalRoom - 1] to [CurrentGlobalRoom + 2] active in the rendering hierarchy.
        /// </summary>
        public void ApplyClientGpuRoomCulling(int localPlayerFloor, int localPlayerRoom)
        {
            int currentGlobalIndex = ((localPlayerFloor - 1) * GameConstants.RoomsPerFloor) + localPlayerRoom;
            int minActiveGlobal = Mathf.Max(1, currentGlobalIndex - GameConstants.ActivePreviousRoomsCount);
            int maxActiveGlobal = Mathf.Min(GameConstants.TotalRooms, currentGlobalIndex + GameConstants.ActiveNextRoomsCount);

            for (int f = 1; f <= GameConstants.TotalFloors; f++)
            {
                for (int r = 1; r <= GameConstants.RoomsPerFloor; r++)
                {
                    var roomCtrl = GetRoom(f, r);
                    if (roomCtrl == null) continue;

                    int globalIdx = roomCtrl.GlobalRoomNumber;
                    bool shouldRender = (globalIdx >= minActiveGlobal && globalIdx <= maxActiveGlobal);

                    if (roomCtrl.gameObject.activeSelf != shouldRender)
                    {
                        roomCtrl.gameObject.SetActive(shouldRender);
                    }
                }
            }
        }

        /// <summary>
        /// Returns all 5 players sorted by highest Floor -> highest Room -> earliest entry timestamp.
        /// Used both for the live HUD Tower Progress Tracker and for Final Match Placement (1st to 5th).
        /// </summary>
        public List<PlayerTowerPosition> GetSortedStandings()
        {
            return _playerPositions.Values
                .OrderByDescending(p => p.Floor)
                .ThenByDescending(p => p.Room)
                .ThenBy(p => p.RoomEntryTimestamp)
                .ToList();
        }

        private void BroadcastStandings()
        {
            OnLeaderboardUpdated?.Invoke(GetSortedStandings());
        }
    }
}
