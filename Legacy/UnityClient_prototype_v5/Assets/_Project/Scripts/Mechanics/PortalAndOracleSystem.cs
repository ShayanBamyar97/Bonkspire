using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Core;

namespace TowerOfChaos.Mechanics
{
    public enum PortalDestinationType
    {
        LadderAscension = 0, // Sends player +1 or +2 Floors UP
        SnakeAbyss = 1       // Drops player -1 or -2 Floors DOWN into a heavily trapped room
    }

    public enum PortalVisualHologramState
    {
        NeutralMystery = 0,          // Default purple cosmic portal (destination unknown)
        SpoofedFakeLadderGreen = 1,  // Sabotaged by a rival to look like a safe +Floor boost!
        SpoofedFakeSnakeRed = 2      // Sabotaged by a rival to scare players away from a real Ladder portal!
    }

    [Serializable]
    public class TowerPortalNode
    {
        public string PortalId;
        public int SourceFloor;
        public int SourceRoom;

        // Authoritative Fixed Destination (Constant for the entire match)
        public PortalDestinationType TrueType;
        public int TargetFloor;
        public int TargetRoom;

        // Public Sabotage State (visible to players who haven't scanned with Gems)
        public PortalVisualHologramState PublicHologramState = PortalVisualHologramState.NeutralMystery;
        public Guid? SabotagedByPlayerId;
        public bool HasKnockbackTrapInFront;
    }

    /// <summary>
    /// Manages the 3D Portals (Modern Snakes & Ladders), the Gem Oracle Scanner
    /// (capped at 3 uses per match per player for fair play), and Portal Sabotage mechanics.
    /// </summary>
    public class PortalAndOracleSystem : MonoBehaviour
    {
        private readonly Dictionary<string, TowerPortalNode> _portalsByRoomKey = new Dictionary<string, TowerPortalNode>();
        private readonly Dictionary<Guid, int> _playerGemScansUsedThisMatch = new Dictionary<Guid, int>();

        public event Action<Guid, TowerPortalNode, int> OnPortalScannedByPlayer; // (PlayerId, TruePortalData, RemainingScans)
        public event Action<Guid, int, int, int> OnNextTwoRoomsScannedByPlayer;  // (PlayerId, CurrentFloor, CurrentRoom, RemainingScans)
        public event Action<Guid, string, PortalVisualHologramState> OnPortalSabotaged;

        private static string GetRoomKey(int floor, int room) => $"F{floor}_R{room}";

        /// <summary>
        /// Deterministically generates 2 to 3 fixed-destination portals per floor using the match seed.
        /// </summary>
        public void GenerateMatchPortals(int matchSeed)
        {
            _portalsByRoomKey.Clear();
            _playerGemScansUsedThisMatch.Clear();

            var rng = new System.Random(matchSeed ^ 0x5A3C91);

            for (int floor = 1; floor <= GameConstants.TotalFloors; floor++)
            {
                // Spawn 2 portals per floor (avoiding Room 1 and Room 10)
                int firstPortalRoom = rng.Next(2, 6);
                int secondPortalRoom = rng.Next(6, 10);

                CreatePortal(floor, firstPortalRoom, rng);
                CreatePortal(floor, secondPortalRoom, rng);
            }
        }

        private void CreatePortal(int sourceFloor, int sourceRoom, System.Random rng)
        {
            // Floor 1 portals always go UP; Floor 10 portals (except Room 100) have higher chance to drop down
            bool isLadder;
            if (sourceFloor == 1)
                isLadder = true;
            else if (sourceFloor == GameConstants.TotalFloors)
                isLadder = false;
            else
                isLadder = rng.NextDouble() >= 0.50;

            int floorDelta = rng.Next(1, 3); // 1 or 2 floors
            int targetFloor = isLadder
                ? Mathf.Min(GameConstants.TotalFloors, sourceFloor + floorDelta)
                : Mathf.Max(1, sourceFloor - floorDelta);

            int targetRoom = rng.Next(2, 9);

            var node = new TowerPortalNode
            {
                PortalId = $"PORTAL_F{sourceFloor}_R{sourceRoom}",
                SourceFloor = sourceFloor,
                SourceRoom = sourceRoom,
                TrueType = isLadder ? PortalDestinationType.LadderAscension : PortalDestinationType.SnakeAbyss,
                TargetFloor = targetFloor,
                TargetRoom = targetRoom,
                PublicHologramState = PortalVisualHologramState.NeutralMystery
            };

            _portalsByRoomKey[GetRoomKey(sourceFloor, sourceRoom)] = node;
        }

        public TowerPortalNode GetPortalInRoom(int floor, int room)
        {
            _portalsByRoomKey.TryGetValue(GetRoomKey(floor, room), out var portal);
            return portal;
        }

        public int GetRemainingGemScans(Guid playerId)
        {
            int used = _playerGemScansUsedThisMatch.TryGetValue(playerId, out int count) ? count : 0;
            return Mathf.Max(0, GameConstants.MaxGemScansPerMatch - used);
        }

        /// <summary>
        /// Authoritative Gem Oracle Scan: Reveals the TRUE fixed destination of a Portal to the scanning player
        /// (even if a rival placed a fake green hologram on it!). Enforces the 3-scans-per-match fair-play cap.
        /// </summary>
        public bool TryScanPortalWithGems(Guid playerId, int floor, int room, ref int playerGemBalance, out TowerPortalNode revealedPortal)
        {
            revealedPortal = null;

            if (GetRemainingGemScans(playerId) <= 0) return false;
            if (playerGemBalance < GameConstants.GemCostPortalScan) return false;

            var portal = GetPortalInRoom(floor, room);
            if (portal == null) return false;

            playerGemBalance -= GameConstants.GemCostPortalScan;
            _playerGemScansUsedThisMatch[playerId] = (_playerGemScansUsedThisMatch.TryGetValue(playerId, out int used) ? used : 0) + 1;

            revealedPortal = portal;
            OnPortalScannedByPlayer?.Invoke(playerId, portal, GetRemainingGemScans(playerId));
            return true;
        }

        /// <summary>
        /// Authoritative Gem X-Ray Vision: Zooms out the camera and reveals all traps and players in the next 2 rooms.
        /// Shares the 3-uses-per-match tactical cap.
        /// </summary>
        public bool TryScanNextTwoRoomsWithGems(Guid playerId, int currentFloor, int currentRoom, ref int playerGemBalance)
        {
            if (GetRemainingGemScans(playerId) <= 0) return false;
            if (playerGemBalance < GameConstants.GemCostXRayTwoRooms) return false;

            playerGemBalance -= GameConstants.GemCostXRayTwoRooms;
            _playerGemScansUsedThisMatch[playerId] = (_playerGemScansUsedThisMatch.TryGetValue(playerId, out int used) ? used : 0) + 1;

            OnNextTwoRoomsScannedByPlayer?.Invoke(playerId, currentFloor, currentRoom, GetRemainingGemScans(playerId));
            return true;
        }

        /// <summary>
        /// Portal Sabotage Mechanic:
        /// Allows a player who scanned a Snake Portal (drop down) to place a Fake Green "Safe Boost (+2 Floors)" hologram
        /// on it (or plant a Repulsor Mine in front of it) to bait and trap rivals!
        /// </summary>
        public bool TrySabotagePortalHologram(Guid playerId, int floor, int room, PortalVisualHologramState fakeState)
        {
            var portal = GetPortalInRoom(floor, room);
            if (portal == null) return false;

            portal.PublicHologramState = fakeState;
            portal.SabotagedByPlayerId = playerId;
            OnPortalSabotaged?.Invoke(playerId, portal.PortalId, fakeState);
            return true;
        }
    }
}
