using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Core;
using TowerOfChaos.Player;

namespace TowerOfChaos.Rooms
{
    /// <summary>
    /// 10 Unique Cinematic Genres for the 10 Floors of the Tower.
    /// Every floor changes lighting, audio, sound effects, trap themes, and creatures!
    /// </summary>
    public enum FloorGenreType
    {
        Floor01_AncientDungeonAdventure = 1, // Classic torches, stone traps, skeleton guards
        Floor02_SlapstickComedyCarnival = 2, // Funny sound effects, banana peels, Farting Gassy Goblins!
        Floor03_PsychologicalHorrorAsylum = 3, // Pitch dark, sudden jump-scare screams, Weeping Stalkers & Screaming Dolls!
        Floor04_SurrealHallucinationMind = 4, // Inverted joystick gas, mirror-flipped camera, swapped reality
        Floor05_SciFiCyberpunkLaserGrid = 5, // Neon lasers, EMP fields, zero-G bubbles, Cyber Enforcers
        Floor06_ToxicJungleSwampSurvival = 6, // Whirlpool water canals with jump pads, Man-Eating Plants
        Floor07_WildWestDynamiteHeist = 7,   // Runaway minecarts, fuse dynamite barrels, skeleton gunslingers
        Floor08_FrozenBlizzardGlacier = 8,   // Slippery ice floor, snowball avalanches, Frost Yetis (snowman trap)
        Floor09_TacticalSpecOpsMilitary = 9, // Laser snipers, Claymore mines, auto-turrets, tear gas
        Floor10_MythicGoldenThroneFinale = 10 // Peak combination of all genres + Golden Sentinels + Room 100 Crown!
    }

    public enum CreatureSpecialBehavior
    {
        StandardMeleeGuard = 0,
        ComedyFartChemicalCloud = 1,   // M01 Gassy Goblin: Farts a green toxic cloud that blurs/waves screen for 4s!
        ComedyCreamPieBlind = 2,       // M02 Pie Jester: Splats white cream on the camera lens for 3s
        HorrorJumpScareScream = 3,     // M04 Screaming Doll: Sits in dark corner, leaps with terrifying 3D scream!
        HorrorWeepingAngelStalker = 4, // M05 Weeping Stalker: Freezes when player faces it; sprints when player turns away!
        SurrealMirrorDoppelganger = 5, // M07 Shadow Doppelganger: Copies player hero appearance
        JungleSwallowAndSpit = 6,      // M09 Man-Eating Plant: Swallows player for 1.5s and spits them backward
        IceSnowmanEncase = 7,          // M10 Frost Yeti: Turns player into a snowman for 2s
        MilitaryLaserSniper = 8,       // M11 Spec-Ops Sniper: Aims red laser line for 1.8s before firing 40 DMG shot
        Room10StaircaseMiniBoss = 9    // Floor 10th Room Guardian protecting the 3D Staircase to the next floor
    }

    /// <summary>
    /// Modular ScriptableObject definition for Room Creatures & Monsters (Live-Ops Update Ready).
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreatureDefinition", menuName = "TowerOfChaos/Modular Creature Definition")]
    public class ScriptableCreatureDefinition : ScriptableObject
    {
        public string CreatureId;
        public string NameFA;
        public string NameEN;
        public FloorGenreType NativeFloorGenre;
        public CreatureSpecialBehavior SpecialBehavior;
        public float MaxHealth = 50f;
        public float MoveSpeed = 4.2f;
        public float AttackDamage = 20f;
        public bool LocksRoomExitUntilKilled = true;
        public bool LeavesAftermathHazardOnDeath; // e.g., Gassy Goblin leaves a 5s fart cloud on death!
        public GameObject CreaturePrefab;
    }

    /// <summary>
    /// Runtime instance of a Creature inside a Room (or the Room 10 Staircase Mini-Boss).
    /// Can be killed by Hero Basic Attacks (0.6s CD), Hero Active Skills, or lured into Room Traps (Instant Kill!).
    /// </summary>
    public class RoomCreatureInstance : MonoBehaviour
    {
        public string CreatureId;
        public FloorGenreType Genre;
        public CreatureSpecialBehavior Behavior;
        public float CurrentHealth = 50f;
        public float MaxHealth = 50f;
        public bool IsRoom10StaircaseGuardian;
        public bool IsDead { get; private set; }

        private Guid _lastAttackerOrLurerPlayerId;

        // Events for UI, Audio (screams / fart sounds!), and Room Door / Staircase unlocking
        public event Action<RoomCreatureInstance, Guid> OnCreatureKilled;
        public event Action<Guid, float> OnChemicalFartCloudTriggered; // Blurs & turns victim's screen green/wavy for 4s!
        public event Action<Guid> OnHorrorJumpScareTriggered;          // Plays terrifying 3D scream & camera shake!

        public void InitializeCreature(
            string creatureId,
            FloorGenreType genre,
            CreatureSpecialBehavior behavior,
            float maxHp,
            bool isRoom10Guardian)
        {
            CreatureId = creatureId;
            Genre = genre;
            Behavior = behavior;
            MaxHealth = maxHp;
            CurrentHealth = maxHp;
            IsRoom10StaircaseGuardian = isRoom10Guardian;
            IsDead = false;
        }

        /// <summary>
        /// Damages the creature via Hero Basic Attack (25 DMG), Skill, or Backpack Weapon.
        /// </summary>
        public void TakeDamageFromPlayer(Guid attackerPlayerId, float damageAmount)
        {
            if (IsDead || damageAmount <= 0f) return;

            _lastAttackerOrLurerPlayerId = attackerPlayerId;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);

            if (CurrentHealth <= 0f)
            {
                KillCreature(_lastAttackerOrLurerPlayerId);
            }
        }

        /// <summary>
        /// Instant Trap-Kill Mechanic:
        /// If a player lures this creature onto a room trap (pendulum, spikes, minecart, red barrel),
        /// the creature is instantly obliterated and unlocks the door/staircase!
        /// </summary>
        public void ExecuteInstantTrapKill(Guid luringPlayerId)
        {
            if (IsDead) return;
            _lastAttackerOrLurerPlayerId = luringPlayerId;
            CurrentHealth = 0f;
            KillCreature(luringPlayerId);
        }

        /// <summary>
        /// Executes the creature's genre-specific special attack against a nearby player.
        /// </summary>
        public void TriggerSpecialAttackOnPlayer(HeroController targetHero)
        {
            if (IsDead || targetHero == null || targetHero.IsDead) return;

            switch (Behavior)
            {
                case CreatureSpecialBehavior.ComedyFartChemicalCloud:
                    // M01 Gassy Goblin: Farts a green toxic cloud -> blurs/waves screen for 4s + cough slow + 10 DMG!
                    targetHero.TakeDamage(10f, isPhysicalTrap: false);
                    OnChemicalFartCloudTriggered?.Invoke(targetHero.PlayerId, 4.0f);
                    break;

                case CreatureSpecialBehavior.HorrorJumpScareScream:
                    // M04 Screaming Doll: Sudden terrifying 3D audio scream + 20 DMG
                    targetHero.TakeDamage(20f, isPhysicalTrap: false);
                    OnHorrorJumpScareTriggered?.Invoke(targetHero.PlayerId);
                    break;

                case CreatureSpecialBehavior.HorrorWeepingAngelStalker:
                    // M05 Weeping Stalker: Only attacks if player is facing AWAY from the statue!
                    Vector3 toCreature = (transform.position - targetHero.transform.position).normalized;
                    float dot = Vector3.Dot(targetHero.transform.forward, toCreature);
                    if (dot < 0f) // Player turned their back to run toward the exit door!
                    {
                        targetHero.TakeDamage(30f, isPhysicalTrap: false);
                        OnHorrorJumpScareTriggered?.Invoke(targetHero.PlayerId);
                    }
                    break;

                case CreatureSpecialBehavior.MilitaryLaserSniper:
                    // M11 Spec-Ops Sniper: High-damage 40 HP tactical shot if player doesn't take cover
                    targetHero.TakeDamage(40f, isPhysicalTrap: false);
                    break;

                default:
                    targetHero.TakeDamage(20f, isPhysicalTrap: true);
                    break;
            }
        }

        private void KillCreature(Guid killerPlayerId)
        {
            IsDead = true;
            OnCreatureKilled?.Invoke(this, killerPlayerId);
        }
    }

    /// <summary>
    /// Manages the 10th Room of each floor ("Staircase Gauntlet Room"):
    /// - 1.5x trap density
    /// - Guarded by a Floor Genre Mini-Boss
    /// - Unlocks the 3D Staircase to Floor N+1 when the Guardian is killed
    /// - Respawns a 60% HP Guardian for chasers arriving 10+ seconds later so Room 10 stays exciting!
    /// </summary>
    public class RoomTenStaircaseController : MonoBehaviour
    {
        public int CurrentFloor = 1;
        public FloorGenreType FloorGenre => (FloorGenreType)Mathf.Clamp(CurrentFloor, 1, 10);
        public bool IsStaircaseGateOpen { get; private set; }
        public RoomCreatureInstance ActiveStaircaseGuardian;

        public event Action<int, Guid> OnStaircaseGateUnlocked;
        public event Action<Guid, int> OnPlayerClimbedStairsToNextFloor;

        public void InitializeRoomTenGauntlet(int floor, RoomCreatureInstance guardianInstance)
        {
            CurrentFloor = floor;
            IsStaircaseGateOpen = false;
            ActiveStaircaseGuardian = guardianInstance;

            if (ActiveStaircaseGuardian != null)
            {
                ActiveStaircaseGuardian.OnCreatureKilled += HandleGuardianDefeated;
            }
        }

        private void HandleGuardianDefeated(RoomCreatureInstance guardian, Guid killerPlayerId)
        {
            IsStaircaseGateOpen = true;
            OnStaircaseGateUnlocked?.Invoke(CurrentFloor, killerPlayerId);
        }

        /// <summary>
        /// Called when a player runs up the unlocked 3D Staircase at the end of Room 10.
        /// Transitions the player to Room 1 of Floor (CurrentFloor + 1),
        /// and respawns a 60% HP Guardian for subsequent players!
        /// </summary>
        public bool TryClimbStaircaseToNextFloor(HeroController hero, TowerManager towerManager)
        {
            if (!IsStaircaseGateOpen || hero == null || hero.IsDead) return false;
            if (CurrentFloor >= GameConstants.TotalFloors) return false;

            int nextFloor = CurrentFloor + 1;
            hero.CurrentFloor = nextFloor;
            hero.CurrentRoom = 1;

            if (towerManager != null)
            {
                towerManager.UpdatePlayerRoomPosition(hero.PlayerId, nextFloor, 1, Time.timeAsDouble);
            }

            OnPlayerClimbedStairsToNextFloor?.Invoke(hero.PlayerId, nextFloor);

            // Re-lock staircase with a 60% HP Guardian for chasers behind the leader!
            if (ActiveStaircaseGuardian != null)
            {
                IsStaircaseGateOpen = false;
                ActiveStaircaseGuardian.InitializeCreature(
                    ActiveStaircaseGuardian.CreatureId,
                    FloorGenre,
                    ActiveStaircaseGuardian.Behavior,
                    ActiveStaircaseGuardian.MaxHealth * 0.60f,
                    isRoom10Guardian: true);
            }

            return true;
        }
    }
}
