-- ============================================================================
-- TOWER OF CHAOS: 100 ROOMS - AUTHORITATIVE POSTGRESQL DATABASE SCHEMA
-- Supports: Accounts, Progression, Heroes & Skins (Arcana), Loadouts,
-- Bilingual Taunt/Voice Wheel, Live-Ops Events, Chests, Match Logs & IAP.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ----------------------------------------------------------------------------
-- 1. USERS & PLAYER PROFILES
-- ----------------------------------------------------------------------------
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(32) NOT NULL UNIQUE,
    display_name VARCHAR(64) NOT NULL,
    email VARCHAR(255) UNIQUE,
    device_id VARCHAR(128),
    google_play_id VARCHAR(128) UNIQUE,
    bazaar_Myket_id VARCHAR(128) UNIQUE,
    preferred_lang VARCHAR(5) NOT NULL DEFAULT 'en' CHECK (preferred_lang IN ('en', 'fa')),
    account_level INT NOT NULL DEFAULT 1 CHECK (account_level >= 1),
    current_xp BIGINT NOT NULL DEFAULT 0 CHECK (current_xp >= 0),
    gold_balance BIGINT NOT NULL DEFAULT 500 CHECK (gold_balance >= 0),
    gem_balance INT NOT NULL DEFAULT 50 CHECK (gem_balance >= 0),
    trophies INT NOT NULL DEFAULT 0 CHECK (trophies >= 0),
    selected_hero_id VARCHAR(32) NOT NULL DEFAULT 'hero_kaveh',
    is_vip_active BOOLEAN NOT NULL DEFAULT FALSE,
    vip_expires_at TIMESTAMPTZ,
    is_banned BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_trophies ON users(trophies DESC);
CREATE INDEX idx_users_level ON users(account_level DESC);

-- ----------------------------------------------------------------------------
-- 2. HEROES & COSMETIC SKINS (DOTA 2 / CLASH MODEL: FIXED BASE STATS)
-- ----------------------------------------------------------------------------
CREATE TABLE heroes (
    hero_id VARCHAR(32) PRIMARY KEY, -- e.g. 'hero_kaveh', 'hero_nyx', 'hero_alcheringa', 'hero_titan', 'hero_astra'
    name_fa VARCHAR(64) NOT NULL,
    name_en VARCHAR(64) NOT NULL,
    role_type VARCHAR(32) NOT NULL,  -- 'TANK', 'ASSASSIN', 'TRAPPER', 'BRAWLER', 'ILLUSIONIST'
    base_hp INT NOT NULL DEFAULT 100,
    base_move_speed NUMERIC(4,2) NOT NULL DEFAULT 6.00,
    passive_id VARCHAR(64) NOT NULL,
    active_skill_id VARCHAR(64) NOT NULL,
    active_cooldown_sec NUMERIC(4,1) NOT NULL DEFAULT 14.0,
    unlock_level INT NOT NULL DEFAULT 1,
    unlock_gold_cost INT NOT NULL DEFAULT 0,
    unlock_gem_cost INT NOT NULL DEFAULT 0
);

CREATE TABLE hero_skins (
    skin_id VARCHAR(64) PRIMARY KEY,
    hero_id VARCHAR(32) NOT NULL REFERENCES heroes(hero_id) ON DELETE CASCADE,
    name_fa VARCHAR(128) NOT NULL,
    name_en VARCHAR(128) NOT NULL,
    rarity VARCHAR(16) NOT NULL CHECK (rarity IN ('DEFAULT', 'RARE', 'EPIC', 'MYTHIC', 'ARCANA')),
    has_custom_skill_vfx BOOLEAN NOT NULL DEFAULT FALSE,
    has_custom_voice_pack BOOLEAN NOT NULL DEFAULT FALSE,
    has_custom_crown_finisher BOOLEAN NOT NULL DEFAULT FALSE,
    gem_price INT,
    unlock_account_level INT
);

CREATE TABLE user_unlocked_heroes (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    hero_id VARCHAR(32) NOT NULL REFERENCES heroes(hero_id) ON DELETE CASCADE,
    equipped_skin_id VARCHAR(64) NOT NULL REFERENCES hero_skins(skin_id),
    unlocked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, hero_id)
);

CREATE TABLE user_unlocked_skins (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    skin_id VARCHAR(64) NOT NULL REFERENCES hero_skins(skin_id) ON DELETE CASCADE,
    obtained_source VARCHAR(32) NOT NULL, -- 'LEVEL_UP', 'SHOP', 'CHEST', 'EVENT_PASS'
    unlocked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, skin_id)
);

-- ----------------------------------------------------------------------------
-- 3. TACTICAL BACKPACK ITEMS (3 PRE-MATCH LOADOUT SLOTS)
-- ----------------------------------------------------------------------------
CREATE TABLE tactical_items (
    item_id VARCHAR(64) PRIMARY KEY, -- e.g. 'item_magnetic_mine', 'item_cryo_grenade', 'item_portal_spoofer'
    name_fa VARCHAR(64) NOT NULL,
    name_en VARCHAR(64) NOT NULL,
    category VARCHAR(32) NOT NULL CHECK (category IN ('DEPLOYABLE_TRAP', 'OFFENSIVE_WEAPON', 'UTILITY_MOBILITY', 'SABOTAGE')),
    charges_per_match INT NOT NULL DEFAULT 2,
    cooldown_sec NUMERIC(4,1) NOT NULL DEFAULT 10.0,
    unlock_level INT NOT NULL DEFAULT 1,
    gold_price INT NOT NULL DEFAULT 500,
    gem_price INT NOT NULL DEFAULT 25
);

CREATE TABLE user_tactical_inventory (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    item_id VARCHAR(64) NOT NULL REFERENCES tactical_items(item_id) ON DELETE CASCADE,
    equipped_slot INT CHECK (equipped_slot IS NULL OR equipped_slot BETWEEN 1 AND 3), -- Max 3 pre-match slots
    unlocked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, item_id),
    CONSTRAINT uq_user_loadout_slot UNIQUE (user_id, equipped_slot)
);

-- ----------------------------------------------------------------------------
-- 4. BILINGUAL TAUNT VOICE LINES & CHAT WHEEL (کل‌کل صوتی و متنی)
-- ----------------------------------------------------------------------------
CREATE TABLE taunt_wheel_items (
    taunt_id VARCHAR(64) PRIMARY KEY,
    taunt_type VARCHAR(16) NOT NULL CHECK (taunt_type IN ('VOICE_LINE', 'TEXT_CHAT', 'HOLOGRAM_EMOTE')),
    text_fa VARCHAR(160) NOT NULL,
    text_en VARCHAR(160) NOT NULL,
    audio_clip_fa_key VARCHAR(128),
    audio_clip_en_key VARCHAR(128),
    rarity VARCHAR(16) NOT NULL DEFAULT 'RARE' CHECK (rarity IN ('COMMON', 'RARE', 'EPIC', 'MYTHIC')),
    unlock_level INT,
    gold_price INT,
    gem_price INT
);

CREATE TABLE user_taunt_wheel (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    taunt_id VARCHAR(64) NOT NULL REFERENCES taunt_wheel_items(taunt_id) ON DELETE CASCADE,
    wheel_slot INT CHECK (wheel_slot IS NULL OR wheel_slot BETWEEN 1 AND 6), -- 6 slots on in-game wheel
    unlocked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, taunt_id),
    CONSTRAINT uq_user_wheel_slot UNIQUE (user_id, wheel_slot)
);

-- ----------------------------------------------------------------------------
-- 5. SEASONAL EVENTS, BATTLE PASS & CHESTS (DOTA 2 / CLASH LIVE-OPS)
-- ----------------------------------------------------------------------------
CREATE TABLE seasonal_events (
    season_id VARCHAR(32) PRIMARY KEY,
    title_fa VARCHAR(128) NOT NULL,
    title_en VARCHAR(128) NOT NULL,
    starts_at TIMESTAMPTZ NOT NULL,
    ends_at TIMESTAMPTZ NOT NULL,
    premium_pass_gem_cost INT NOT NULL DEFAULT 450,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE user_season_progress (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    season_id VARCHAR(32) NOT NULL REFERENCES seasonal_events(season_id) ON DELETE CASCADE,
    season_points INT NOT NULL DEFAULT 0,
    current_tier INT NOT NULL DEFAULT 1 CHECK (current_tier BETWEEN 1 AND 50),
    has_premium_pass BOOLEAN NOT NULL DEFAULT FALSE,
    claimed_free_tiers INT[] NOT NULL DEFAULT '{}',
    claimed_premium_tiers INT[] NOT NULL DEFAULT '{}',
    PRIMARY KEY (user_id, season_id)
);

CREATE TABLE user_chests (
    chest_instance_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    chest_type VARCHAR(32) NOT NULL CHECK (chest_type IN ('WOODEN', 'SILVER', 'GOLDEN', 'MYTHIC', 'ARCANA_VAULT')),
    is_opened BOOLEAN NOT NULL DEFAULT FALSE,
    obtained_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    opened_at TIMESTAMPTZ
);

-- ----------------------------------------------------------------------------
-- 6. 5-PLAYER MATCH HISTORY & ANTI-CHEAT AUDIT LOGS
-- ----------------------------------------------------------------------------
CREATE TABLE matches (
    match_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    server_region VARCHAR(32) NOT NULL DEFAULT 'ir-thr-1',
    random_seed BIGINT NOT NULL, -- Seed used to generate traps and fixed portal destinations
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    duration_sec INT,
    winner_user_id UUID REFERENCES users(id)
);

CREATE TABLE match_participants (
    match_id UUID NOT NULL REFERENCES matches(match_id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    hero_id VARCHAR(32) NOT NULL REFERENCES heroes(hero_id),
    opening_dice_roll INT NOT NULL CHECK (opening_dice_roll BETWEEN 1 AND 6),
    final_placement INT NOT NULL CHECK (final_placement BETWEEN 1 AND 5),
    final_floor INT NOT NULL CHECK (final_floor BETWEEN 1 AND 10),
    final_room INT NOT NULL CHECK (final_room BETWEEN 1 AND 10),
    first_discoverer_rooms_count INT NOT NULL DEFAULT 0,
    traps_triggered_on_rivals INT NOT NULL DEFAULT 0,
    portals_sabotaged_count INT NOT NULL DEFAULT 0,
    gem_scans_used INT NOT NULL DEFAULT 0 CHECK (gem_scans_used BETWEEN 0 AND 3), -- Hard cap of 3 per match!
    deaths_count INT NOT NULL DEFAULT 0,
    xp_awarded INT NOT NULL DEFAULT 0,
    gold_awarded INT NOT NULL DEFAULT 0,
    trophy_delta INT NOT NULL DEFAULT 0,
    PRIMARY KEY (match_id, user_id)
);

-- ----------------------------------------------------------------------------
-- 7. IN-APP PURCHASES (CAFE BAZAAR / MYKET / GOOGLE PLAY BILLING)
-- ----------------------------------------------------------------------------
CREATE TABLE iap_transactions (
    transaction_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    store_provider VARCHAR(32) NOT NULL CHECK (store_provider IN ('CAFE_BAZAAR', 'MYKET', 'GOOGLE_PLAY')),
    store_order_id VARCHAR(128) NOT NULL UNIQUE,
    product_sku VARCHAR(64) NOT NULL,
    amount_irr BIGINT,
    amount_usd NUMERIC(8,2),
    gems_granted INT NOT NULL DEFAULT 0,
    verified_by_server BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ----------------------------------------------------------------------------
-- 8. SEED LAUNCH DATA (5 HEROES & CORE TACTICAL ITEMS)
-- ----------------------------------------------------------------------------
INSERT INTO heroes (hero_id, name_fa, name_en, role_type, base_hp, base_move_speed, passive_id, active_skill_id, active_cooldown_sec, unlock_level) VALUES
('hero_kaveh',      'کاوه (آهنگر سپردار)',   'Kaveh (Shieldbreaker)', 'TANK',        100, 5.80, 'passive_iron_armor',      'skill_shield_charge',  14.0, 1),
('hero_nyx',        'سایه (شکارچی شب)',      'Nyx (Nightstalker)',    'ASSASSIN',    100, 6.40, 'passive_silent_step',     'skill_phantom_dash',   15.0, 1),
('hero_alcheringa', 'کیمیا (مهندس تله)',     'Alcheringa (Trapper)',  'TRAPPER',     100, 6.00, 'passive_mech_eye',        'skill_sabotage_drone', 16.0, 3),
('hero_titan',      'رستم (پهلوان نبرد)',    'Titan (Arena Brawler)', 'BRAWLER',     100, 6.00, 'passive_battle_fury',     'skill_gravity_lasso',  13.0, 5),
('hero_astra',      'میترا (جادوگر توهم)',   'Astra (Mirage Weaver)', 'ILLUSIONIST', 100, 6.10, 'passive_clear_mind',      'skill_illusion_pulse', 18.0, 8);

INSERT INTO tactical_items (item_id, name_fa, name_en, category, charges_per_match, cooldown_sec, unlock_level, gold_price, gem_price) VALUES
('item_magnetic_mine',  'مین مغناطیسی',         'Magnetic Mine',   'DEPLOYABLE_TRAP',  3, 8.0,  1, 0,   0),
('item_cryo_grenade',   'نارنجک یخ‌زن',         'Cryo Grenade',    'OFFENSIVE_WEAPON', 2, 10.0, 1, 0,   0),
('item_grappling_hook', 'چنگک سرعت',            'Grappling Hook',  'UTILITY_MOBILITY', 2, 12.0, 1, 0,   0),
('item_portal_spoofer', 'هولوگرام فریب پورتال', 'Portal Spoofer',  'SABOTAGE',         2, 15.0, 2, 600, 30),
('item_repulsor_mine',  'مین پرتاب‌کننده',      'Repulsor Mine',   'DEPLOYABLE_TRAP',  2, 10.0, 4, 800, 40),
('item_smoke_bomb',     'بمب دودزا و کورکننده', 'Abyssal Smoke',   'SABOTAGE',         2, 14.0, 6, 900, 45),
('item_emp_kit',        'کیت خنثی‌ساز تله',     'EMP Disarm Kit',  'UTILITY_MOBILITY', 2, 16.0, 7, 1200, 60);
