using System;
using System.Collections.Generic;
using UnityEngine;
using TowerOfChaos.Core;

namespace TowerOfChaos.Social
{
    [Serializable]
    public class BilingualTauntEntry
    {
        public string TauntId;
        public string TextFA;
        public string TextEN;
        public string AudioKeyFA;
        public string AudioKeyEN;
        public int UnlockAccountLevel;
    }

    /// <summary>
    /// Handles Bilingual Localization (Persian RTL & English LTR) and the 6-Slot
    /// In-Game Voice & Chat Wheel (کل‌کل صوتی و متنی حین بازی) inspired by Dota 2 & Clash.
    /// </summary>
    public class LocalizationAndTauntSystem : MonoBehaviour
    {
        public LanguageCode CurrentLanguage = LanguageCode.FA;
        public float TauntCooldownSec = 3.5f;
        private float _lastTauntTime = -10f;

        private readonly Dictionary<string, BilingualTauntEntry> _tauntCatalog = new Dictionary<string, BilingualTauntEntry>();
        public readonly string[] EquippedWheelSlotIds = new string[6];

        // Fired when any player on the same floor triggers a Voice Line or Chat Taunt
        public event Action<Guid, int, string, string> OnFloorTauntBroadcast; // (SenderId, Floor, LocalizedText, AudioClipKey)

        private void Awake()
        {
            SeedDefaultTauntCatalog();
        }

        private void SeedDefaultTauntCatalog()
        {
            RegisterTaunt(new BilingualTauntEntry
            {
                TauntId = "taunt_portal_bye",
                TextFA = "پورتالِ خوبی بود، طبقه اول سلام برسون!",
                TextEN = "Nice portal! Say hello to Floor 1 for me!",
                AudioKeyFA = "vo_fa_portal_bye",
                AudioKeyEN = "vo_en_portal_bye",
                UnlockAccountLevel = 1
            });

            RegisterTaunt(new BilingualTauntEntry
            {
                TauntId = "taunt_first_runner",
                TextFA = "نفر اول منم، بفرمایید تله!",
                TextEN = "First runner takes the loot—enjoy my traps!",
                AudioKeyFA = "vo_fa_first_runner",
                AudioKeyEN = "vo_en_first_runner",
                UnlockAccountLevel = 1
            });

            RegisterTaunt(new BilingualTauntEntry
            {
                TauntId = "taunt_crown_waiting",
                TextFA = "عجله نکن، تاج رو برات نگه داشتم!",
                TextEN = "Take your time, I'm keeping the Crown warm!",
                AudioKeyFA = "vo_fa_crown_waiting",
                AudioKeyEN = "vo_en_crown_waiting",
                UnlockAccountLevel = 3
            });

            RegisterTaunt(new BilingualTauntEntry
            {
                TauntId = "taunt_inverted_Controls",
                TextFA = "ببخشید، جهت‌هات برعکس شد؟!",
                TextEN = "Oops, did your controls just flip upside down?!",
                AudioKeyFA = "vo_fa_inverted",
                AudioKeyEN = "vo_en_inverted",
                UnlockAccountLevel = 5
            });

            RegisterTaunt(new BilingualTauntEntry
            {
                TauntId = "taunt_room_one_respawn",
                TextFA = "بازم اتاق اول؟ خسته نشدی؟!",
                TextEN = "Back to Room 1 again? Getting dizzy yet?!",
                AudioKeyFA = "vo_fa_room_one",
                AudioKeyEN = "vo_en_room_one",
                UnlockAccountLevel = 10
            });

            RegisterTaunt(new BilingualTauntEntry
            {
                TauntId = "taunt_gg_ez",
                TextFA = "صد تا اتاق بود یا صد تا شوخی؟!",
                TextEN = "Was that 100 rooms or a morning walk?!",
                AudioKeyFA = "vo_fa_100_rooms",
                AudioKeyEN = "vo_en_100_rooms",
                UnlockAccountLevel = 15
            });

            EquippedWheelSlotIds[0] = "taunt_portal_bye";
            EquippedWheelSlotIds[1] = "taunt_first_runner";
            EquippedWheelSlotIds[2] = "taunt_crown_waiting";
            EquippedWheelSlotIds[3] = "taunt_inverted_Controls";
            EquippedWheelSlotIds[4] = "taunt_room_one_respawn";
            EquippedWheelSlotIds[5] = "taunt_gg_ez";
        }

        public void RegisterTaunt(BilingualTauntEntry entry)
        {
            _tauntCatalog[entry.TauntId] = entry;
        }

        /// <summary>
        /// Broadcasts a Voice & Text Taunt from one of the 6 Wheel Slots (0..5) to all players on the same floor.
        /// Automatically resolves Persian (FA) or English (EN) text and voice audio clip key.
        /// </summary>
        public bool TryTriggerTauntSlot(Guid senderPlayerId, int currentFloor, int wheelSlotIndex)
        {
            if (wheelSlotIndex < 0 || wheelSlotIndex >= EquippedWheelSlotIds.Length) return false;
            if (Time.time - _lastTauntTime < TauntCooldownSec) return false;

            string tauntId = EquippedWheelSlotIds[wheelSlotIndex];
            if (string.IsNullOrEmpty(tauntId) || !_tauntCatalog.TryGetValue(tauntId, out var entry)) return false;

            _lastTauntTime = Time.time;

            string localizedText = (CurrentLanguage == LanguageCode.FA) ? entry.TextFA : entry.TextEN;
            string audioKey = (CurrentLanguage == LanguageCode.FA) ? entry.AudioKeyFA : entry.AudioKeyEN;

            OnFloorTauntBroadcast?.Invoke(senderPlayerId, currentFloor, localizedText, audioKey);
            return true;
        }
    }
}
