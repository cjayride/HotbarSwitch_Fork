using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HotbarSwitch {
    [BepInPlugin("cjayride.HotbarSwitch", "Hotbar Switch", "0.5.3")]
    public class BepInExPlugin : BaseUnityPlugin {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> rowsToSwitch;
        public static ConfigEntry<string> hotKey;

        internal static int activeRow;
        private Harmony harmony;
        private static KeyCode cachedKey;
        private static bool cachedKeyValid;
        private static MethodInfo updateIcons;

        private void Awake() {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            rowsToSwitch = Config.Bind<int>("General", "RowsToSwitch", 2, "How many inventory rows the hotkey cycles as the hotbar. Items stay in their original slots.");
            hotKey = Config.Bind<string>("General", "HotKey", "`", "Hotkey to switch which inventory row is shown on the hotbar. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            CacheHotKey();
            hotKey.SettingChanged += (sender, args) => CacheHotKey();
            updateIcons = AccessTools.Method(typeof(HotkeyBar), "UpdateIcons", new Type[] { typeof(Player) });

            if (!modEnabled.Value)
                return;

            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        private void OnDestroy() {
            if (harmony != null)
                harmony.UnpatchSelf();
        }

        private static void CacheHotKey() {
            cachedKeyValid = false;
            if (hotKey == null || string.IsNullOrEmpty(hotKey.Value))
                return;
            string value = hotKey.Value.Trim();
            if (value == "`") {
                cachedKey = KeyCode.BackQuote;
                cachedKeyValid = true;
                return;
            }
            cachedKeyValid = Enum.TryParse(value, true, out cachedKey);
        }

        private void Update() {
            if (!modEnabled.Value || Player.m_localPlayer == null)
                return;
            bool pressed = cachedKeyValid ? Input.GetKeyDown(cachedKey) : AedenthornUtils.CheckKeyDown(hotKey.Value);
            if (!pressed || AedenthornUtils.IgnoreKeyPresses(false))
                return;

            int rows = Math.Max(1, Math.Min(Player.m_localPlayer.GetInventory().GetHeight(), rowsToSwitch.Value));
            activeRow = (activeRow + 1) % rows;
            RefreshHotkeyBars();
        }

        private static void RefreshHotkeyBars() {
            if (updateIcons == null || Player.m_localPlayer == null)
                return;
            HotkeyBar[] bars = Resources.FindObjectsOfTypeAll<HotkeyBar>();
            object[] args = new object[] { Player.m_localPlayer };
            for (int i = 0; i < bars.Length; i++) {
                HotkeyBar bar = bars[i];
                if (bar != null && bar.isActiveAndEnabled)
                    updateIcons.Invoke(bar, args);
            }
        }

        internal static int HotbarY() {
            if (!modEnabled.Value || Player.m_localPlayer == null)
                return 0;
            int rows = Math.Max(1, Math.Min(Player.m_localPlayer.GetInventory().GetHeight(), rowsToSwitch.Value));
            if (activeRow >= rows)
                activeRow = 0;
            return activeRow;
        }

        private static void FillRow(Inventory inventory, List<ItemDrop.ItemData> dest, bool includeEmpty) {
            dest.Clear();
            int row = HotbarY();
            int width = inventory.GetWidth();
            for (int x = 0; x < width; x++) {
                ItemDrop.ItemData item = inventory.GetItemAt(x, row);
                if (item != null || includeEmpty)
                    dest.Add(item);
            }
        }

        [HarmonyPatch(typeof(Inventory), "GetBoundItems")]
        private static class GetBoundItems_Patch {
            private static bool Prefix(Inventory __instance, List<ItemDrop.ItemData> bound) {
                if (HotbarY() <= 0)
                    return true;
                FillRow(__instance, bound, false);
                return false;
            }
        }

        [HarmonyPatch(typeof(Inventory), "GetHotbar", new Type[] { typeof(bool) })]
        private static class GetHotbar_Patch {
            private static bool Prefix(Inventory __instance, bool includeEmpty, ref List<ItemDrop.ItemData> __result) {
                if (HotbarY() <= 0)
                    return true;
                List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
                FillRow(__instance, list, includeEmpty);
                __result = list;
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), "UseHotbarItem")]
        private static class UseHotbarItem_Patch {
            private static bool Prefix(Player __instance, int index) {
                ItemDrop.ItemData item = __instance.GetInventory().GetItemAt(index - 1, HotbarY());
                if (item != null)
                    __instance.UseItem(null, item, false);
                return false;
            }
        }

    }
}
