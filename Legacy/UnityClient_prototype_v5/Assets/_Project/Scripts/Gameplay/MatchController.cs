using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Core;
using TowerOfChaos.Player;

namespace TowerOfChaos.Gameplay
{
    public enum MatchPhase
    {
        WaitingForPlayers = 0,
        OpeningDiceRoll = 1,
        RealTimeTowerRace = 2,
        FinalCrownUnlockInProgress = 3,
        MatchCompleted = 4
    }

    [Serializable]
    public struct PlayerMatchResult
    {
        public Guid PlayerId;
        public string DisplayName;
        public int FinalPlacement; // 1 to 5
        public int FinalFloor;
        public int FinalRoom;
        public int OpeningDiceRoll;
        public int XpAwarded;
        public int GoldAwarded;
        public int TrophyDelta;
    }

    /// <summary>
    /// Authoritative 5-Player Match State Machine.
    /// Manages the Opening Dice Roll (1-6), Real-Time Tower Race,
    /// the 8-second Final Crown Unlock in Floor 10 Room 10 (with damage interrupt and global alert),
    /// and final 1-5 ranking & progression rewards.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        [Header("References")]
        public TowerManager Tower;

        [Header("Match State")]
        public MatchPhase CurrentPhase = MatchPhase.WaitingForPlayers;
        public int AuthoritativeSeed;

        private readonly Dictionary<Guid, HeroController> _players = new Dictionary<Guid, HeroController>();
        private readonly Dictionary<Guid, int> _openingDiceRolls = new Dictionary<Guid, int>();

        // Room 100 (Floor 10, Room 10) 8-Second Crown Capture State
        private Guid? _crownCapturingPlayerId;
        private float _crownHoldTimerSec;

        public event Action<Dictionary<Guid, int>> OnOpeningDiceRolled;
        public event Action<Guid, float> OnGlobalCrownCaptureAlert; // Broadcasts to all 5 players when someone starts the 8s hold in Room 100
        public event Action<Guid> OnCrownCaptureInterrupted;
        public event Action<List<PlayerMatchResult>> OnMatchFinished;

        public void StartFivePlayerMatch(List<HeroController> connectedPlayers, int matchSeed)
        {
            if (connectedPlayers == null || connectedPlayers.Count == 0) return;

            AuthoritativeSeed = matchSeed;
            _players.Clear();
            _openingDiceRolls.Clear();

            foreach (var hero in connectedPlayers)
            {
                _players[hero.PlayerId] = hero;
                hero.OnDamageTakenInterrupt += HandlePlayerDamagedDuringCrownHold;
            }

            ExecuteOpeningDiceRollPhase();
        }

        /// <summary>
        /// Phase 1: Rolls the Opening Dice (1-6) for all 5 players.
        /// Spawns each player in Floor 1, Room [DiceValue].
        /// If two players roll the same number, they spawn together in that room and clash immediately!
        /// Players rolling 1-5 have access to the Starter Loot Chests in Rooms 1-5 to balance the distance gap.
        /// </summary>
        private void ExecuteOpeningDiceRollPhase()
        {
            CurrentPhase = MatchPhase.OpeningDiceRoll;
            var rng = new System.Random(AuthoritativeSeed);

            foreach (var kvp in _players)
            {
                Guid playerId = kvp.Key;
                HeroController hero = kvp.Value;

                int diceValue = rng.Next(GameConstants.MinDiceValue, GameConstants.MaxDiceValue + 1); // 1..6
                _openingDiceRolls[playerId] = diceValue;

                hero.CurrentFloor = 1;
                hero.CurrentRoom = diceValue;

                if (Tower != null)
                {
                    Tower.UpdatePlayerRoomPosition(playerId, 1, diceValue, Time.timeAsDouble);
                }
            }

            OnOpeningDiceRolled?.Invoke(new Dictionary<Guid, int>(_openingDiceRolls));
            CurrentPhase = MatchPhase.RealTimeTowerRace;
        }

        private void Update()
        {
            if (CurrentPhase == MatchPhase.FinalCrownUnlockInProgress && _crownCapturingPlayerId.HasValue)
            {
                var holderId = _crownCapturingPlayerId.Value;
                if (!_players.TryGetValue(holderId, out var hero) || hero.IsDead || hero.CurrentFloor != 10 || hero.CurrentRoom != 10)
                {
                    InterruptCrownCapture(holderId);
                    return;
                }

                _crownHoldTimerSec += Time.deltaTime;
                if (_crownHoldTimerSec >= GameConstants.FinalCrownUnlockHoldSec)
                {
                    CompleteMatchWithCrownWinner(holderId);
                }
            }
        }

        /// <summary>
        /// Called when the leading player reaches Floor 10, Room 10 (Room 100) and holds the Crown Vault switch.
        /// Triggers an 8-second countdown and alerts all players on Floor 10 / across the tower!
        /// </summary>
        public bool TryBeginFinalCrownCapture(Guid playerId)
        {
            if (CurrentPhase != MatchPhase.RealTimeTowerRace) return false;
            if (!_players.TryGetValue(playerId, out var hero) || hero.IsDead) return false;
            if (hero.CurrentFloor != GameConstants.TotalFloors || hero.CurrentRoom != GameConstants.RoomsPerFloor) return false;

            _crownCapturingPlayerId = playerId;
            _crownHoldTimerSec = 0f;
            CurrentPhase = MatchPhase.FinalCrownUnlockInProgress;

            OnGlobalCrownCaptureAlert?.Invoke(playerId, GameConstants.FinalCrownUnlockHoldSec);
            return true;
        }

        private void HandlePlayerDamagedDuringCrownHold(Guid damagedPlayerId)
        {
            if (CurrentPhase == MatchPhase.FinalCrownUnlockInProgress && _crownCapturingPlayerId == damagedPlayerId)
            {
                InterruptCrownCapture(damagedPlayerId);
            }
        }

        public void InterruptCrownCapture(Guid interruptedPlayerId)
        {
            _crownCapturingPlayerId = null;
            _crownHoldTimerSec = 0f;
            CurrentPhase = MatchPhase.RealTimeTowerRace;
            OnCrownCaptureInterrupted?.Invoke(interruptedPlayerId);
        }

        /// <summary>
        /// Immediately ends the match once the 8-second Crown hold completes.
        /// Ranks players 1 to 5 based on Winner + highest Floor/Room reached at that exact moment.
        /// </summary>
        private void CompleteMatchWithCrownWinner(Guid winnerId)
        {
            CurrentPhase = MatchPhase.MatchCompleted;
            _crownCapturingPlayerId = null;

            var sortedStandings = Tower != null ? Tower.GetSortedStandings() : new List<PlayerTowerPosition>();
            var results = new List<PlayerMatchResult>(GameConstants.PlayersPerMatch);

            // Ensure Crown Capturer is strictly #1
            int rankIndex = 0;
            foreach (var pos in sortedStandings)
            {
                if (!_players.TryGetValue(pos.PlayerId, out var hero)) continue;

                int placement = rankIndex + 1;
                int rewardIdx = Mathf.Clamp(rankIndex, 0, GameConstants.PlacementXpRewards.Length - 1);

                results.Add(new PlayerMatchResult
                {
                    PlayerId = pos.PlayerId,
                    DisplayName = hero.DisplayName,
                    FinalPlacement = placement,
                    FinalFloor = pos.Floor,
                    FinalRoom = pos.Room,
                    OpeningDiceRoll = _openingDiceRolls.TryGetValue(pos.PlayerId, out int roll) ? roll : 1,
                    XpAwarded = GameConstants.PlacementXpRewards[rewardIdx],
                    GoldAwarded = GameConstants.PlacementGoldRewards[rewardIdx],
                    TrophyDelta = GameConstants.PlacementTrophyDelta[rewardIdx]
                });

                rankIndex++;
            }

            OnMatchFinished?.Invoke(results);
        }
    }
}
