using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Rooms;

namespace TowerOfChaos.Data
{
    [Serializable]
    public class FloorGenreMetadata
    {
        public int FloorNumber;
        public FloorGenreType Genre;
        public string TitleFA;
        public string TitleEN;
        public Color AmbientLightColor;
        public Color FogColor;
        public string RegularCreatureId;
        public string Room10MiniBossId;
    }

    [Serializable]
    public class CreatureCatalogEntry
    {
        public string CreatureId;
        public int FloorNumber;
        public bool IsRoom10StaircaseMiniBoss;
        public string NameFA;
        public string NameEN;
        public float MaxHp;
        public float Damage;
        public CreatureSpecialBehavior SpecialBehavior;
    }

    [Serializable]
    public class TrapCatalogEntry
    {
        public string TrapId;
        public string NameFA;
        public string NameEN;
        public TrapCategory Category;
        public int NativeFloor;
        public float Damage;
        public int ChargesIn5PlayerMode; // -1 = Permanent/Infinite
        public bool IsStaticImpassable;
    }

    [Serializable]
    public class RewardCatalogEntry
    {
        public string RewardId;
        public string NameFA;
        public string NameEN;
        public string DescriptionFA;
    }

    /// <summary>
    /// Complete Data Registry containing all 10 Floor Genres, all 20 Creatures & Mini-Bosses (M01-M20),
    /// all 60 Creative Traps (T01-T60), and all 20 First-Runner Rewards (R01-R20) from SPEC.md v5.0.
    /// Designed to be extended via JSON / Addressables in future seasonal updates.
    /// </summary>
    public static class MasterGameCatalog
    {
        public static readonly List<FloorGenreMetadata> Floors = new List<FloorGenreMetadata>
        {
            new FloorGenreMetadata { FloorNumber = 1,  Genre = FloorGenreType.Floor01_AncientDungeonAdventure,   TitleFA = "طبقه ۱: سیاه‌چال باستانی",          TitleEN = "Floor 1: Ancient Dungeon",       RegularCreatureId = "M01", Room10MiniBossId = "M02" },
            new FloorGenreMetadata { FloorNumber = 2,  Genre = FloorGenreType.Floor02_SlapstickComedyCarnival,   TitleFA = "طبقه ۲: کارناوال کمدی و شیمیایی",   TitleEN = "Floor 2: Crazy Gag Carnival",    RegularCreatureId = "M03", Room10MiniBossId = "M05" },
            new FloorGenreMetadata { FloorNumber = 3,  Genre = FloorGenreType.Floor03_PsychologicalHorrorAsylum, TitleFA = "طبقه ۳: تیمارستان وحشت و تاریکی",   TitleEN = "Floor 3: Dark Horror Asylum",    RegularCreatureId = "M07", Room10MiniBossId = "M08" },
            new FloorGenreMetadata { FloorNumber = 4,  Genre = FloorGenreType.Floor04_SurrealHallucinationMind,  TitleFA = "طبقه ۴: سرزمین توهم و جنون",        TitleEN = "Floor 4: Surreal Mindfuck",      RegularCreatureId = "M09", Room10MiniBossId = "M10" },
            new FloorGenreMetadata { FloorNumber = 5,  Genre = FloorGenreType.Floor05_SciFiCyberpunkLaserGrid,   TitleFA = "طبقه ۵: آزمایشگاه سایبرپانک",       TitleEN = "Floor 5: Cyberpunk Laser Grid",  RegularCreatureId = "M11", Room10MiniBossId = "M12" },
            new FloorGenreMetadata { FloorNumber = 6,  Genre = FloorGenreType.Floor06_ToxicJungleSwampSurvival,  TitleFA = "طبقه ۶: مرداب وحشی و اسیدی",        TitleEN = "Floor 6: Toxic Swamp Jungle",    RegularCreatureId = "M13", Room10MiniBossId = "M14" },
            new FloorGenreMetadata { FloorNumber = 7,  Genre = FloorGenreType.Floor07_WildWestDynamiteHeist,     TitleFA = "طبقه ۷: قطار وسترن و دینامیت",      TitleEN = "Floor 7: Wild West Minecart",    RegularCreatureId = "M15", Room10MiniBossId = "M16" },
            new FloorGenreMetadata { FloorNumber = 8,  Genre = FloorGenreType.Floor08_FrozenBlizzardGlacier,     TitleFA = "طبقه ۸: قلعه یخبندان و کولاک",      TitleEN = "Floor 8: Frozen Blizzard",       RegularCreatureId = "M17", Room10MiniBossId = "M18" },
            new FloorGenreMetadata { FloorNumber = 9,  Genre = FloorGenreType.Floor09_TacticalSpecOpsMilitary,   TitleFA = "طبقه ۹: میدان نبرد نظامی ویژه",     TitleEN = "Floor 9: Spec-Ops Hellfire",     RegularCreatureId = "M19", Room10MiniBossId = "M20" },
            new FloorGenreMetadata { FloorNumber = 10, Genre = FloorGenreType.Floor10_MythicGoldenThroneFinale,  TitleFA = "طبقه ۱۰: عرش طلایی آشوب",           TitleEN = "Floor 10: Golden Throne Finale", RegularCreatureId = "M19", Room10MiniBossId = "M20" }
        };

        public static readonly List<CreatureCatalogEntry> Creatures = new List<CreatureCatalogEntry>
        {
            new CreatureCatalogEntry { CreatureId = "M01", FloorNumber = 1,  IsRoom10StaircaseMiniBoss = false, NameFA = "اسکلت سپردار",             NameEN = "Shield Skeleton",     MaxHp = 50f,  Damage = 15f, SpecialBehavior = CreatureSpecialBehavior.StandardMeleeGuard },
            new CreatureCatalogEntry { CreatureId = "M02", FloorNumber = 1,  IsRoom10StaircaseMiniBoss = true,  NameFA = "گولم سنگی پله",            NameEN = "Stone Gargoyle",      MaxHp = 125f, Damage = 25f, SpecialBehavior = CreatureSpecialBehavior.Room10StaircaseMiniBoss },
            new CreatureCatalogEntry { CreatureId = "M03", FloorNumber = 2,  IsRoom10StaircaseMiniBoss = false, NameFA = "غول لوبیاخور گوزو",        NameEN = "Gassy Goblin",        MaxHp = 50f,  Damage = 10f, SpecialBehavior = CreatureSpecialBehavior.ComedyFartChemicalCloud },
            new CreatureCatalogEntry { CreatureId = "M04", FloorNumber = 2,  IsRoom10StaircaseMiniBoss = false, NameFA = "دلقک کیک‌پران",            NameEN = "Cream Pie Jester",    MaxHp = 50f,  Damage = 5f,  SpecialBehavior = CreatureSpecialBehavior.ComedyCreamPieBlind },
            new CreatureCatalogEntry { CreatureId = "M05", FloorNumber = 2,  IsRoom10StaircaseMiniBoss = true,  NameFA = "سرآشپز دیوانه لوبیاپزی",   NameEN = "Chef Flatulence",     MaxHp = 150f, Damage = 20f, SpecialBehavior = CreatureSpecialBehavior.ComedyFartChemicalCloud },
            new CreatureCatalogEntry { CreatureId = "M06", FloorNumber = 3,  IsRoom10StaircaseMiniBoss = false, NameFA = "عروسک جیغ‌کش تاریکی",      NameEN = "Screaming Doll",      MaxHp = 50f,  Damage = 20f, SpecialBehavior = CreatureSpecialBehavior.HorrorJumpScareScream },
            new CreatureCatalogEntry { CreatureId = "M07", FloorNumber = 3,  IsRoom10StaircaseMiniBoss = false, NameFA = "شبح مجسمه گریان",          NameEN = "Weeping Stalker",     MaxHp = 75f,  Damage = 30f, SpecialBehavior = CreatureSpecialBehavior.HorrorWeepingAngelStalker },
            new CreatureCatalogEntry { CreatureId = "M08", FloorNumber = 3,  IsRoom10StaircaseMiniBoss = true,  NameFA = "جلاد زنجیردار تیمارستان",  NameEN = "The Butcher",         MaxHp = 150f, Damage = 35f, SpecialBehavior = CreatureSpecialBehavior.HorrorJumpScareScream },
            new CreatureCatalogEntry { CreatureId = "M09", FloorNumber = 4,  IsRoom10StaircaseMiniBoss = false, NameFA = "همزاد سایه‌ای",            NameEN = "Shadow Doppelganger", MaxHp = 50f,  Damage = 15f, SpecialBehavior = CreatureSpecialBehavior.SurrealMirrorDoppelganger },
            new CreatureCatalogEntry { CreatureId = "M10", FloorNumber = 4,  IsRoom10StaircaseMiniBoss = true,  NameFA = "ارباب هزارچهره سراب",      NameEN = "Mirage Weaver",       MaxHp = 125f, Damage = 25f, SpecialBehavior = CreatureSpecialBehavior.SurrealMirrorDoppelganger },
            new CreatureCatalogEntry { CreatureId = "M11", FloorNumber = 5,  IsRoom10StaircaseMiniBoss = false, NameFA = "پهپاد شوکر سایبری",        NameEN = "Cyber Enforcer",      MaxHp = 75f,  Damage = 15f, SpecialBehavior = CreatureSpecialBehavior.StandardMeleeGuard },
            new CreatureCatalogEntry { CreatureId = "M12", FloorNumber = 5,  IsRoom10StaircaseMiniBoss = true,  NameFA = "مکعب هسته‌ای نگهبان",      NameEN = "Omega Core",          MaxHp = 150f, Damage = 30f, SpecialBehavior = CreatureSpecialBehavior.Room10StaircaseMiniBoss },
            new CreatureCatalogEntry { CreatureId = "M13", FloorNumber = 6,  IsRoom10StaircaseMiniBoss = false, NameFA = "گیاه گوشت‌خوار غول‌پیکر",  NameEN = "Venus Man-Trap",      MaxHp = 75f,  Damage = 20f, SpecialBehavior = CreatureSpecialBehavior.JungleSwallowAndSpit },
            new CreatureCatalogEntry { CreatureId = "M14", FloorNumber = 6,  IsRoom10StaircaseMiniBoss = true,  NameFA = "پادشاه تمساح‌های اسیدی",   NameEN = "Swamp Leviathan",     MaxHp = 175f, Damage = 30f, SpecialBehavior = CreatureSpecialBehavior.JungleSwallowAndSpit },
            new CreatureCatalogEntry { CreatureId = "M15", FloorNumber = 7,  IsRoom10StaircaseMiniBoss = false, NameFA = "یاغی دینامیت‌انداز",       NameEN = "Mad Bomber Bandit",   MaxHp = 50f,  Damage = 25f, SpecialBehavior = CreatureSpecialBehavior.StandardMeleeGuard },
            new CreatureCatalogEntry { CreatureId = "M16", FloorNumber = 7,  IsRoom10StaircaseMiniBoss = true,  NameFA = "کلانتر اسکلتی لوکوموتیو",  NameEN = "Sheriff Ironjaw",     MaxHp = 150f, Damage = 30f, SpecialBehavior = CreatureSpecialBehavior.Room10StaircaseMiniBoss },
            new CreatureCatalogEntry { CreatureId = "M17", FloorNumber = 8,  IsRoom10StaircaseMiniBoss = false, NameFA = "یتی برف‌انداز",            NameEN = "Frost Yeti",          MaxHp = 75f,  Damage = 15f, SpecialBehavior = CreatureSpecialBehavior.IceSnowmanEncase },
            new CreatureCatalogEntry { CreatureId = "M18", FloorNumber = 8,  IsRoom10StaircaseMiniBoss = true,  NameFA = "غول یخی وایکینگ",          NameEN = "Frostmourne Jarl",    MaxHp = 175f, Damage = 35f, SpecialBehavior = CreatureSpecialBehavior.IceSnowmanEncase },
            new CreatureCatalogEntry { CreatureId = "M19", FloorNumber = 9,  IsRoom10StaircaseMiniBoss = false, NameFA = "تک‌تیرانداز لیزری کماندو", NameEN = "Spec-Ops Sniper",     MaxHp = 75f,  Damage = 40f, SpecialBehavior = CreatureSpecialBehavior.MilitaryLaserSniper },
            new CreatureCatalogEntry { CreatureId = "M20", FloorNumber = 10, IsRoom10StaircaseMiniBoss = true,  NameFA = "تورت زرهی جاگرنات",        NameEN = "Juggernaut Mech",     MaxHp = 200f, Damage = 40f, SpecialBehavior = CreatureSpecialBehavior.Room10StaircaseMiniBoss }
        };

        public static readonly List<RewardCatalogEntry> Rewards = new List<RewardCatalogEntry>
        {
            new RewardCatalogEntry { RewardId = "R01", NameFA = "اکسیر حیات",             NameEN = "Vitality Orb",        DescriptionFA = "+30 HP بازیابی جان" },
            new RewardCatalogEntry { RewardId = "R02", NameFA = "گنبد تیتانیوم",          NameEN = "Titanium Shield",     DescriptionFA = "+40 Shield محافظ" },
            new RewardCatalogEntry { RewardId = "R03", NameFA = "چکمه‌های شبح‌آسا",       NameEN = "Ghostwalker Boots",   DescriptionFA = "۶ ثانیه سرعت +30% و راه رفتن روی آب و تیغ" },
            new RewardCatalogEntry { RewardId = "R04", NameFA = "پرتابگر صعود",           NameEN = "Mini-Rocket Boost",   DescriptionFA = "پرواز تا جلوی در خروجی اتاق" },
            new RewardCatalogEntry { RewardId = "R05", NameFA = "ماسک ضدگاز و ضدتوهم",   NameEN = "Clarity & Gas Mask",  DescriptionFA = "خنثی‌سازی گاز شیمیایی غول گوزو و توهمات" },
            new RewardCatalogEntry { RewardId = "R06", NameFA = "جعبه مهمات تصادفی",      NameEN = "Tactical Supply Box", DescriptionFA = "+1 سلاح یا تله در کوله‌پشتی" },
            new RewardCatalogEntry { RewardId = "R07", NameFA = "اورشارژ مهارت",          NameEN = "Skill Overcharge",    DescriptionFA = "صفر شدن فوری کول‌داون اسکیل هیرو" },
            new RewardCatalogEntry { RewardId = "R08", NameFA = "زره بازتابنده تیغ",      NameEN = "Thorn Mail",          DescriptionFA = "بازتاب ۵۰٪ دمیج رقبا به خودشان" },
            new RewardCatalogEntry { RewardId = "R09", NameFA = "پهپاد محافظ خودکار",     NameEN = "Auto-Sentry Orb",     DescriptionFA = "۱۲ ثانیه شلیک خودکار به هیولاها و رقبا" },
            new RewardCatalogEntry { RewardId = "R10", NameFA = "توکن رایگان چشم بصیرت", NameEN = "Free Oracle Token",   DescriptionFA = "+1 اسکن رایگان پورتال/اتاق بدون کسر جم" },
            new RewardCatalogEntry { RewardId = "R11", NameFA = "قطب‌نمای پورتال",        NameEN = "Portal Compass",      DescriptionFA = "نمایش صعودی یا سقوطی بودن پورتال طبقه" },
            new RewardCatalogEntry { RewardId = "R12", NameFA = "نفرین پیشتاز",           NameEN = "Leader's Curse",      DescriptionFA = "زدن صاعقه کندکننده به نفر اول فعلی مسابقه" },
            new RewardCatalogEntry { RewardId = "R13", NameFA = "کیسه سکه و جم",          NameEN = "Treasure Jackpot",    DescriptionFA = "+25 سکه طلا یا +3 جم دائمی" },
            new RewardCatalogEntry { RewardId = "R14", NameFA = "کلید طلایی",             NameEN = "Master Key",          DescriptionFA = "عبور بدون توقف از قفل ۵ ثانیه‌ای PvP یا قفل هیولا" },
            new RewardCatalogEntry { RewardId = "R15", NameFA = "بسته تله دوبل",          NameEN = "Double Trap Charge",  DescriptionFA = "+1 شارژ برای تمام تله‌های کوله‌پشتی" },
            new RewardCatalogEntry { RewardId = "R16", NameFA = "مشت آهنین هیولاکُش",     NameEN = "Monster Slayer",      DescriptionFA = "کشتن هر هیولای عادی با ۱ ضربه به مدت ۱۵ ثانیه" },
            new RewardCatalogEntry { RewardId = "R17", NameFA = "تله‌ربای مغناطیسی",      NameEN = "Trap Stealer",        DescriptionFA = "دزدیدن اولین تله کاشته‌شده رقیب به داخل کوله‌پشتی خود" },
            new RewardCatalogEntry { RewardId = "R18", NameFA = "ردپای نامرئی",           NameEN = "Stealth Tracker",     DescriptionFA = "مخفی شدن موقعیت شما روی مینی‌مپ ۱۰ طبقه برای ۱۲ ثانیه" },
            new RewardCatalogEntry { RewardId = "R19", NameFA = "شربت غول‌پیکری",         NameEN = "Titan Elixir",        DescriptionFA = "۶ ثانیه سایز بزرگ و مصونیت کامل از پرتاب شدن" },
            new RewardCatalogEntry { RewardId = "R20", NameFA = "بلیت آسانسور پله",       NameEN = "Staircase Sprint",    DescriptionFA = "۲ برابر شدن سرعت دویدن روی پله‌های اتاق ۱۰" }
        };
    }
}
