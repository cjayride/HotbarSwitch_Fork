using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HotbarSwitch {
    [BepInPlugin("cjayride.HotbarSwitch", "Hotbar Switch", "0.5.2")]
    public class BepInExPlugin : BaseUnityPlugin {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> rowsToSwitch;
        public static ConfigEntry<string> hotKey;

        internal static int activeRow;
        private static bool readingHotbar;
        private Harmony harmony;

        private static FieldInfo elementsField;
        private static FieldInfo iconField;
        private static FieldInfo amountField;
        private static FieldInfo durabilityField;
        private static FieldInfo equippedField;
        private static PropertyInfo amountText;
        private static MethodInfo durabilitySetValue;

        private void Awake() {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            rowsToSwitch = Config.Bind<int>("General", "RowsToSwitch", 2, "How many inventory rows the hotkey cycles as the hotbar. Items stay in their original slots.");
            hotKey = Config.Bind<string>("General", "HotKey", "`", "Hotkey to switch which inventory row is shown on the hotbar. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");

            Type elementType = AccessTools.Inner(typeof(HotkeyBar), "ElementData");
            if (elementType == null)
                elementType = AccessTools.Inner(typeof(HotkeyBar), "Element");
            elementsField = AccessTools.Field(typeof(HotkeyBar), "m_elements");
            if (elementType != null) {
                iconField = AccessTools.Field(elementType, "m_icon");
                amountField = AccessTools.Field(elementType, "m_amount");
                durabilityField = AccessTools.Field(elementType, "m_durability");
                equippedField = AccessTools.Field(elementType, "m_equiped");
                if (equippedField == null)
                    equippedField = AccessTools.Field(elementType, "m_equipped");
                if (amountField != null)
                    amountText = amountField.FieldType.GetProperty("text");
                if (durabilityField != null)
                    durabilitySetValue = AccessTools.Method(durabilityField.FieldType, "SetValue", new Type[] { typeof(float) });
            }

            if (!modEnabled.Value)
                return;

            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        private void OnDestroy() {
            if (harmony != null)
                harmony.UnpatchSelf();
        }

        private void Update() {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(false) || !AedenthornUtils.CheckKeyDown(hotKey.Value))
                return;
            if (Player.m_localPlayer == null)
                return;

            int rows = Math.Max(1, Math.Min(Player.m_localPlayer.GetInventory().GetHeight(), rowsToSwitch.Value));
            activeRow = (activeRow + 1) % rows;
        }

        private void LateUpdate() {
            if (!modEnabled.Value || Player.m_localPlayer == null)
                return;
            PaintHotkeyBars(Player.m_localPlayer);
        }

        internal static int HotbarY() {
            if (!modEnabled.Value || Player.m_localPlayer == null)
                return 0;
            int rows = Math.Max(1, Math.Min(Player.m_localPlayer.GetInventory().GetHeight(), rowsToSwitch.Value));
            if (activeRow >= rows)
                activeRow = 0;
            return activeRow;
        }

        private static HotkeyBar[] cachedBars;
        private static float cachedBarsAt;

        private static void PaintHotkeyBars(Player player) {
            if (elementsField == null || iconField == null)
                return;
            Inventory inventory = player.GetInventory();
            int row = HotbarY();
            int width = inventory.GetWidth();
            if (cachedBars == null || Time.unscaledTime - cachedBarsAt > 2f) {
                cachedBars = Resources.FindObjectsOfTypeAll<HotkeyBar>();
                cachedBarsAt = Time.unscaledTime;
            }
            HotkeyBar[] bars = cachedBars;
            for (int b = 0; b < bars.Length; b++) {
                HotkeyBar bar = bars[b];
                if (bar == null || !bar.isActiveAndEnabled)
                    continue;
                IList elements = elementsField.GetValue(bar) as IList;
                if (elements == null)
                    continue;
                if (!IsMainHotkeyBar(bar, elements.Count, width))
                    continue;
                int count = elements.Count;
                if (count > width)
                    count = width;
                for (int x = 0; x < count; x++)
                    PaintSlot(elements[x], inventory.GetItemAt(x, row));
            }
        }

        private static bool IsMainHotkeyBar(HotkeyBar bar, int elementCount, int width) {
            if (elementCount < width)
                return false;
            Transform t = bar.transform;
            for (int i = 0; i < 6 && t != null; i++) {
                string n = t.name;
                if (n != null) {
                    string lower = n.ToLower();
                    if (lower.IndexOf("quick") >= 0 || lower.IndexOf("extra") >= 0 || lower.IndexOf("equip") >= 0)
                        return false;
                }
                t = t.parent;
            }
            return true;
        }

        private static void PaintSlot(object element, ItemDrop.ItemData item) {
            if (element == null)
                return;
            Image icon = iconField.GetValue(element) as Image;
            if (icon != null) {
                if (item == null) {
                    icon.enabled = false;
                    icon.sprite = null;
                }
                else {
                    icon.enabled = true;
                    icon.sprite = item.GetIcon();
                }
            }
            if (amountField != null && amountText != null) {
                object amount = amountField.GetValue(element);
                if (amount != null)
                    amountText.SetValue(amount, (item != null && item.m_stack > 1) ? item.m_stack.ToString() : "", null);
            }
            if (durabilityField != null) {
                Component durability = durabilityField.GetValue(element) as Component;
                if (durability != null) {
                    bool show = item != null && item.m_shared.m_useDurability;
                    durability.gameObject.SetActive(show);
                    if (show && durabilitySetValue != null)
                        durabilitySetValue.Invoke(durability, new object[] { item.GetDurabilityPercentage() });
                }
            }
            if (equippedField != null) {
                GameObject equipped = equippedField.GetValue(element) as GameObject;
                if (equipped != null)
                    equipped.SetActive(item != null && item.m_equipped);
            }
        }

        [HarmonyPatch(typeof(Inventory), "GetItemAt", new Type[] { typeof(int), typeof(int) })]
        private static class GetItemAt_Patch {
            private static void Prefix(ref int y) {
                if (readingHotbar && y == 0)
                    y = HotbarY();
            }
        }

        [HarmonyPatch(typeof(Inventory), "GetBoundItems")]
        private static class GetBoundItems_Patch {
            private static bool Prefix(Inventory __instance, List<ItemDrop.ItemData> bound) {
                int row = HotbarY();
                if (row <= 0)
                    return true;
                bound.Clear();
                int width = __instance.GetWidth();
                for (int x = 0; x < width; x++) {
                    ItemDrop.ItemData item = __instance.GetItemAt(x, row);
                    if (item != null)
                        bound.Add(item);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), "UseHotbarItem")]
        private static class UseHotbarItem_Patch {
            private static void Prefix() {
                readingHotbar = true;
            }
            private static void Postfix() {
                readingHotbar = false;
            }
        }

        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        private static class UpdateIcons_Patch {
            private static void Prefix() {
                readingHotbar = true;
            }
            private static void Postfix() {
                readingHotbar = false;
                if (Player.m_localPlayer != null)
                    PaintHotkeyBars(Player.m_localPlayer);
            }
        }
    }
}
