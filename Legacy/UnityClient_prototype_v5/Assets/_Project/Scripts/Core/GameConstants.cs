using System;

namespace TowerOfChaos.Core
{
    /// <summary>
    /// Immutable gameplay, balance, and economy constants as defined in SPEC.md.
    /// </summary>
    public static class GameConstants
    {
        // --- Tower Architecture (10x10 = 100 Rooms) ---
        public const int TotalFloors = 10;
        public const int RoomsPerFloor = 10;
        public const int TotalRooms = TotalFloors * RoomsPerFloor; // 100 Rooms
        public const int PlayersPerMatch = 5;

        // --- GPU Room Streaming & Culling ---
        public const int ActivePreviousRoomsCount = 1;
        public const int ActiveNextRoomsCount = 2;

        // --- Opening Dice & Floor 1 Balancing ---
        public const int MinDiceValue = 1;
        public const int MaxDiceValue = 6;
        public const int GuaranteedStarterLootMaxRoom = 5; // Rooms 1..5 of Floor 1 have Starter Chests

        // --- Hero Health, Shield & Respawn Rules ---
        public const float BaseHeroHealth = 100f;
        public const float MaxHeroShield = 50f;
        public const float StandardDeathAnimDurationSec = 2.0f;
        public const float RoomOneRespawnPenaltyMinSec = 3.0f;
        public const float RoomOneRespawnPenaltyMaxSec = 5.0f;

        // --- Doors, Lockdowns & Victory Condition ---
        public const float PvPRoomLockDurationSec = 5.0f;
        public const float FloorAscensionElevatorSec = 3.0f;
        public const float FinalCrownUnlockHoldSec = 8.0f;

        // --- Hallucination & Sensory Trap Durations ---
        public const float InvertedControlsDurationSec = 6.0f;
        public const float AbyssalBlindnessDurationSec = 5.0f;
        public const float BlindnessVisionRadiusMeters = 1.5f;

        // --- Backpack & Gem Oracle Fair-Play Caps ---
        public const int PreMatchLoadoutSlots = 3;
        public const int InMatchLootSlots = 2;
        public const int MaxGemScansPerMatch = 3; // Strict competitive cap per player per match
        public const int GemCostPortalScan = 5;
        public const int GemCostXRayTwoRooms = 8;

        // --- 3D Brawl-Stars Top-Down Camera Angles & Transitions ---
        public const float CameraBlendDurationSec = 0.4f;
        public const float StandardTopDownPitchDegrees = 54f; // 3D Top-Down like Brawl Stars
        public const float CombatTacticalPitchDegrees = 58f;  // Wider 3D Top-Down during PvP / Portal
        public const float XRayOverviewPitchDegrees = 62f;    // Two-Room X-Ray Scan overview
        public const float PortalCameraZoomTriggerRadius = 3.0f;

        // --- End-of-Match Progression Rewards (Placements 1 to 5) ---
        public static readonly int[] PlacementXpRewards   = { 150, 100, 70, 40, 25 };
        public static readonly int[] PlacementGoldRewards = { 100, 65,  40, 20, 10 };
        public static readonly int[] PlacementTrophyDelta = { +30, +15, +5, -5, -15 };
    }

    public enum FloorBiome
    {
        IronDungeon = 0,        // Floors 1-2: Physical traps (pendulums, floor spikes)
        VenomAndMirage = 1,     // Floors 3-4: Hallucination traps (inverted controls, blindness, mirage doors)
        ClockworkForge = 2,     // Floors 5-6: Kinetic & speed traps (crushers, moving tiles, bumpers)
        VoidAndMirror = 3,      // Floors 7-8: High portal density, shadow clones, gravity shifts
        TheGoldenSpire = 4      // Floors 9-10: Peak intensity combined traps + high-stakes PvP
    }

    public enum HeroArchetype
    {
        Kaveh_Tank,             // Passive: -20% physical trap dmg | Active: Shield Charge
        Nyx_Assassin,           // Passive: +0.5s delay on player traps | Active: Phantom Dash (Invis + Speed)
        Alcheringa_Trapper,     // Passive: Reveals hidden traps & mirage doors | Active: Sabotage Drone
        Titan_Brawler,          // Passive: +15% speed/dmg when >=2 players in room | Active: Gravity Lasso
        Astra_Illusionist       // Passive: -50% hallucination duration | Active: Illusion Pulse (Inverts rivals)
    }

    public enum LanguageCode
    {
        FA, // Persian (RTL)
        EN  // English (LTR)
    }
}
