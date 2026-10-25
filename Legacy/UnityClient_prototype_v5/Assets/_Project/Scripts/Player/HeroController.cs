using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Core;
using TowerOfChaos.Rooms;

namespace TowerOfChaos.Player
{
    /// <summary>
    /// Authoritative & Client-Predicted 3D Hero Controller.
    /// Implements the 5 Launch Hero Archetypes, Hallucination status effects (Inverted Controls, Blindness),
    /// Backpack (3 Core Loadout slots + 2 In-Match Loot slots), and exact Floor Respawn & Loot Drop rules.
    /// </summary>
    public class HeroController : MonoBehaviour
    {
        [Header("Player Identity & Archetype")]
        public Guid PlayerId;
        public string DisplayName;
        public HeroArchetype Archetype = HeroArchetype.Kaveh_Tank;
        public string EquippedSkinId = "default";

        [Header("Current Tower Position")]
        public int CurrentFloor = 1;
        public int CurrentRoom = 1;

        [Header("Health & Shield (Dota 2 Fair Base Stats)")]
        public float CurrentHealth = GameConstants.BaseHeroHealth;
        public float CurrentShield = 0f;
        public bool IsDead { get; private set; }
        private float _respawnTimerRemainingSec;

        [Header("Movement & Active Skill")]
        public float BaseMoveSpeed = 6.0f;
        public float ActiveSkillCooldownSec = 14.0f;
        private float _skillCooldownRemainingSec;
        private bool _isShieldCharging;
        private bool _isPhantomInvisible;

        [Header("Hallucination Status Effects")]
        public bool AreControlsInverted { get; private set; }
        private float _invertedControlsRemainingSec;
        public bool IsBlindnessActive { get; private set; }
        private float _blindnessRemainingSec;

        [Header("Tactical Backpack (3 Core Loadout + 2 In-Match Loot)")]
        public List<string> CoreLoadoutItems = new List<string>(GameConstants.PreMatchLoadoutSlots);
        public List<string> InMatchLootedItems = new List<string>(GameConstants.InMatchLootSlots);

        private TowerManager _towerManager;

        public event Action<Guid, float, float> OnVitalsChanged; // (PlayerId, HP, Shield)
        public event Action<Guid> OnDamageTakenInterrupt;        // Interrupts 8s Crown unlock in Room 100!
        public event Action<Guid, int, int, float, string> OnPlayerDied; // (PlayerId, RespawnFloor, RespawnRoom, LockoutSec, DroppedLootId)
        public event Action<Guid, int, int> OnPlayerRespawned;

        public void InitializeHero(Guid playerId, string displayName, HeroArchetype archetype, List<string> preMatchLoadout, TowerManager towerManager)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            Archetype = archetype;
            _towerManager = towerManager;

            CurrentHealth = GameConstants.BaseHeroHealth;
            CurrentShield = 0f;
            IsDead = false;

            CoreLoadoutItems.Clear();
            if (preMatchLoadout != null)
            {
                for (int i = 0; i < Mathf.Min(preMatchLoadout.Count, GameConstants.PreMatchLoadoutSlots); i++)
                {
                    CoreLoadoutItems.Add(preMatchLoadout[i]);
                }
            }
            InMatchLootedItems.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Handle Respawn Lockout Countdown
            if (IsDead)
            {
                _respawnTimerRemainingSec -= dt;
                if (_respawnTimerRemainingSec <= 0f)
                {
                    CompleteRespawn();
                }
                return;
            }

            // Skill Cooldown
            if (_skillCooldownRemainingSec > 0f)
            {
                _skillCooldownRemainingSec = Mathf.Max(0f, _skillCooldownRemainingSec - dt);
            }

            // Hallucination: Inverted Controls Timer
            if (AreControlsInverted)
            {
                _invertedControlsRemainingSec -= dt;
                if (_invertedControlsRemainingSec <= 0f)
                {
                    AreControlsInverted = false;
                }
            }

            // Hallucination: Abyssal Blindness Timer
            if (IsBlindnessActive)
            {
                _blindnessRemainingSec -= dt;
                if (_blindnessRemainingSec <= 0f)
                {
                    IsBlindnessActive = false;
                }
            }
        }

        /// <summary>
        /// Processes 3D character movement from the virtual joystick, applying Hallucination Inversion
        /// and Hero Passive speed modifiers (e.g. Titan's +15% Battle Fury when >=2 players are in the room).
        /// </summary>
        public Vector3 ComputeWorldMovementDelta(Vector2 rawJoystickInput, float deltaTime)
        {
            if (IsDead) return Vector3.zero;

            // Apply Hallucination Inversion if active!
            Vector2 effectiveInput = AreControlsInverted ? -rawJoystickInput : rawJoystickInput;
            if (effectiveInput.sqrMagnitude > 1f)
            {
                effectiveInput.Normalize();
            }

            float speedMultiplier = 1.0f;

            // Passive: Titan (Rostam) gains +15% speed when >=2 players are in the current room
            if (Archetype == HeroArchetype.Titan_Brawler && _towerManager != null)
            {
                var room = _towerManager.GetRoom(CurrentFloor, CurrentRoom);
                if (room != null && room.ActivePlayerCount >= 2)
                {
                    speedMultiplier += 0.15f;
                }
            }

            if (_isPhantomInvisible)
            {
                speedMultiplier += 0.40f; // Nyx Active Skill: +40% speed
            }

            Vector3 moveDir = new Vector3(effectiveInput.x, 0f, effectiveInput.y);
            return moveDir * (BaseMoveSpeed * speedMultiplier * deltaTime);
        }

        /// <summary>
        /// Applies Hallucination Status Effects (with Astra's -50% Clear Mind passive reduction).
        /// </summary>
        public void ApplyHallucinationEffect(bool invertControls, bool applyBlindness)
        {
            if (IsDead) return;

            // Passive: Astra (Mitra) reduces hallucination durations by 50%
            float durationMultiplier = (Archetype == HeroArchetype.Astra_Illusionist) ? 0.5f : 1.0f;

            if (invertControls)
            {
                AreControlsInverted = true;
                _invertedControlsRemainingSec = GameConstants.InvertedControlsDurationSec * durationMultiplier;
            }

            if (applyBlindness)
            {
                IsBlindnessActive = true;
                _blindnessRemainingSec = GameConstants.AbyssalBlindnessDurationSec * durationMultiplier;
            }
        }

        /// <summary>
        /// Applies damage from traps or rival attacks. Interrupts Room 100 Crown capture immediately.
        /// </summary>
        public void TakeDamage(float rawDamage, bool isPhysicalTrap)
        {
            if (IsDead || rawDamage <= 0f) return;

            // Kaveh Shield Charge grants immunity to floor traps while dashing
            if (_isShieldCharging && isPhysicalTrap) return;

            float finalDamage = rawDamage;

            // Passive: Kaveh takes 20% less damage from physical/mechanical traps
            if (isPhysicalTrap && Archetype == HeroArchetype.Kaveh_Tank)
            {
                finalDamage *= 0.80f;
            }

            // Interrupt any active interaction (including the 8-second Crown capture in Room 100!)
            OnDamageTakenInterrupt?.Invoke(PlayerId);

            // Absorb with Shield first
            if (CurrentShield > 0f)
            {
                float absorbed = Mathf.Min(CurrentShield, finalDamage);
                CurrentShield -= absorbed;
                finalDamage -= absorbed;
            }

            if (finalDamage > 0f)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);
            }

            OnVitalsChanged?.Invoke(PlayerId, CurrentHealth, CurrentShield);

            if (CurrentHealth <= 0f)
            {
                ExecuteDeathAndRespawnRules();
            }
        }

        public void HealAndShield(float healAmount, float shieldAmount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Min(GameConstants.BaseHeroHealth, CurrentHealth + healAmount);
            CurrentShield = Mathf.Min(GameConstants.MaxHeroShield, CurrentShield + shieldAmount);
            OnVitalsChanged?.Invoke(PlayerId, CurrentHealth, CurrentShield);
        }

        public bool TryPickUpInMatchLoot(string itemId)
        {
            if (InMatchLootedItems.Count >= GameConstants.InMatchLootSlots)
            {
                return false;
            }
            InMatchLootedItems.Add(itemId);
            return true;
        }

        /// <summary>
        /// Implements the exact Death & Respawn Specification:
        /// 1. Dying in Rooms 2..10 of Floor N -> Respawn in Room 1 of Floor N (after 2s death anim).
        /// 2. Dying in Room 1 of Floor N -> Do NOT drop to Floor N-1; stay in Room 1 of Floor N with a 3-5s Respawn Penalty Timer.
        /// 3. Drop 1 random In-Match Looted item on the floor; keep all 3 Core Loadout items intact.
        /// </summary>
        private void ExecuteDeathAndRespawnRules()
        {
            IsDead = true;
            AreControlsInverted = false;
            IsBlindnessActive = false;

            // 1. Loot Drop Penalty: Drop 1 In-Match Looted item on the floor (Core Loadout stays intact!)
            string droppedLootId = null;
            if (InMatchLootedItems.Count > 0)
            {
                int randomIdx = UnityEngine.Random.Range(0, InMatchLootedItems.Count);
                droppedLootId = InMatchLootedItems[randomIdx];
                InMatchLootedItems.RemoveAt(randomIdx);

                if (_towerManager != null)
                {
                    var deathRoom = _towerManager.GetRoom(CurrentFloor, CurrentRoom);
                    if (deathRoom != null)
                    {
                        deathRoom.SpawnDroppedLootOnFloor(droppedLootId);
                    }
                }
            }

            // 2. Determine Respawn Room & Penalty Timer
            int targetRespawnFloor = CurrentFloor; // Never drops to lower floor on death
            int targetRespawnRoom = 1;             // Always resets to Room 1 of current floor

            if (CurrentRoom == 1)
            {
                // Died in Room 1: Stay in Room 1 of current floor, apply 3 to 5 second Respawn Lockout Penalty
                _respawnTimerRemainingSec = UnityEngine.Random.Range(
                    GameConstants.RoomOneRespawnPenaltyMinSec,
                    GameConstants.RoomOneRespawnPenaltyMaxSec);
            }
            else
            {
                // Died in Rooms 2..10: Reset to Room 1 of current floor after standard 2s death animation
                _respawnTimerRemainingSec = GameConstants.StandardDeathAnimDurationSec;
            }

            CurrentFloor = targetRespawnFloor;
            CurrentRoom = targetRespawnRoom;

            OnPlayerDied?.Invoke(PlayerId, targetRespawnFloor, targetRespawnRoom, _respawnTimerRemainingSec, droppedLootId);
        }

        private void CompleteRespawn()
        {
            IsDead = false;
            CurrentHealth = GameConstants.BaseHeroHealth;
            CurrentShield = 0f;

            if (_towerManager != null)
            {
                _towerManager.UpdatePlayerRoomPosition(PlayerId, CurrentFloor, CurrentRoom, Time.timeAsDouble);
            }

            OnVitalsChanged?.Invoke(PlayerId, CurrentHealth, CurrentShield);
            OnPlayerRespawned?.Invoke(PlayerId, CurrentFloor, CurrentRoom);
        }

        /// <summary>
        /// Triggers the Hero's unique Active Skill if off cooldown.
        /// </summary>
        public bool TryActivateHeroSkill()
        {
            if (IsDead || _skillCooldownRemainingSec > 0f) return false;

            _skillCooldownRemainingSec = ActiveSkillCooldownSec;
            return true;
        }
    }
}
