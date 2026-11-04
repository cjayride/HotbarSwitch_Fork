using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HotbarSwitch {
    [BepInPlugin("cjayride.HotbarSwitch", "Hotbar Switch", "0.5.9")]
    [BepInDependency("seneaL.valheim.ui", BepInDependency.DependencyFlags.SoftDependency)]
    public class BepInExPlugin : BaseUnityPlugin {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> rowsToSwitch;
        public static ConfigEntry<KeyboardShortcut> hotKey;

        internal static int activeRow;
        private Harmony harmony;
        private static bool readingHotbar;
        private static FieldInfo seneaSlots;
        private static MethodInfo seneaSet;
        private static MethodInfo seneaClear;
        private static readonly object[] seneaSetArgs = new object[1];
        private static bool seneaPatched;
        private static bool keyHeld;
        private static object seneaView;

        private void Awake() {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            rowsToSwitch = Config.Bind<int>("General", "RowsToSwitch", 2, "How many inventory rows the hotkey cycles as the hotbar. Items stay in their original slots.");
            hotKey = Config.Bind("General", "HotKey", new KeyboardShortcut(KeyCode.BackQuote),
                "Hotkey to switch which inventory row is shown on the hotbar. Accepts any Unity KeyCode (for example Keypad2) and modifier combos (Ctrl/Alt/Shift). With Configuration Manager, click the field and press the key, or use the key dropdown.");

            if (!modEnabled.Value)
                return;

            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            PatchSeneaL();
        }

        private void Start() {
            PatchSeneaL();
        }

        private void OnDestroy() {
            if (harmony != null)
                harmony.UnpatchSelf();
        }

        private void PatchSeneaL() {
            if (seneaPatched || harmony == null)
                return;
            Type hotbarView = AccessTools.TypeByName("SeneaLUI.Hud.HotbarView");
            Type slotType = AccessTools.TypeByName("SeneaLUI.Components.Slot");
            if (hotbarView == null)
                return;
            seneaSlots = AccessTools.Field(hotbarView, "_slots");
            if (slotType != null) {
                seneaSet = AccessTools.Method(slotType, "Set", new Type[] { typeof(ItemDrop.ItemData) });
                seneaClear = AccessTools.Method(slotType, "Clear");
            }
            MethodInfo tick = AccessTools.Method(hotbarView, "Tick");
            if (tick == null)
                return;
            HarmonyMethod start = new HarmonyMethod(typeof(BepInExPlugin), nameof(BeginHotbarRead));
            HarmonyMethod stop = new HarmonyMethod(typeof(BepInExPlugin), nameof(AfterSeneaLTick));
            harmony.Patch(tick, prefix: start, postfix: stop);
            MethodInfo padTick = AccessTools.Method(hotbarView, "PadTick");
            if (padTick != null)
                harmony.Patch(padTick, prefix: start, postfix: stop);
            seneaPatched = true;
            Logger.LogInfo("SeneaL hotbar hooked.");
        }

        private static void BeginHotbarRead() {
            readingHotbar = true;
        }

        private static void AfterSeneaLTick(object __instance) {
            seneaView = __instance;
            try {
                PaintSeneaLHotbar(__instance);
            }
            finally {
                readingHotbar = false;
            }
        }

        private static void PaintSeneaLHotbar(object view) {
            if (view == null || seneaSlots == null || Player.m_localPlayer == null)
                return;
            Array slots = seneaSlots.GetValue(view) as Array;
            if (slots == null)
                return;
            Inventory inventory = Player.m_localPlayer.GetInventory();
            int row = HotbarY();
            int width = inventory.GetWidth();
            int count = slots.Length;
            if (count > width)
                count = width;
            for (int x = 0; x < count; x++) {
                object slot = slots.GetValue(x);
                if (slot == null)
                    continue;
                ItemDrop.ItemData item = inventory.GetItemAt(x, row);
                if (item != null && seneaSet != null) {
                    seneaSetArgs[0] = item;
                    seneaSet.Invoke(slot, seneaSetArgs);
                }
                else if (seneaClear != null)
                    seneaClear.Invoke(slot, null);
            }
        }

        private void Update() {
            TrySwitchRow();
        }

        private void LateUpdate() {
            if (!seneaPatched)
                PatchSeneaL();
            if (seneaView != null)
                PaintSeneaLHotbar(seneaView);
        }

        private void TrySwitchRow() {
            if (!modEnabled.Value || Player.m_localPlayer == null) {
                keyHeld = false;
                return;
            }
            bool down = ShortcutIsDown(hotKey.Value);
            bool pressed = down && !keyHeld;
            keyHeld = down;
            if (!pressed || AedenthornUtils.IgnoreKeyPresses(false))
                return;

            int rows = Math.Max(1, Math.Min(Player.m_localPlayer.GetInventory().GetHeight(), rowsToSwitch.Value));
            activeRow = (activeRow + 1) % rows;
        }

        private static bool ShortcutIsDown(KeyboardShortcut shortcut) {
            KeyCode main = shortcut.MainKey;
            if (main == KeyCode.None)
                main = KeyCode.BackQuote;
            foreach (KeyCode modifier in shortcut.Modifiers) {
                if (modifier == KeyCode.None)
                    continue;
                if (!KeyHeld(modifier))
                    return false;
            }
            if (KeyDown(main) || KeyHeld(main))
                return true;
            if (main == KeyCode.BackQuote || main == KeyCode.Tilde)
                return KeyDown(KeyCode.BackQuote) || KeyDown(KeyCode.Tilde) || KeyHeld(KeyCode.BackQuote) || KeyHeld(KeyCode.Tilde) || Input.GetKey("`") || Input.GetKey("~");
            return false;
        }

        private static bool KeyDown(KeyCode key) {
            try {
                if (ZInput.GetKeyDown(key, false))
                    return true;
            }
            catch {
            }
            return Input.GetKeyDown(key);
        }

        private static bool KeyHeld(KeyCode key) {
            try {
                if (ZInput.GetKey(key, false))
                    return true;
            }
            catch {
            }
            return Input.GetKey(key);
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
                if (!readingHotbar)
                    return true;
                FillRow(__instance, bound, false);
                return false;
            }
        }

        [HarmonyPatch(typeof(Inventory), "GetHotbar", new Type[] { typeof(bool) })]
        private static class GetHotbar_Patch {
            private static bool Prefix(Inventory __instance, bool includeEmpty, ref List<ItemDrop.ItemData> __result) {
                if (!readingHotbar)
                    return true;
                List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
                FillRow(__instance, list, includeEmpty);
                __result = list;
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
            }
        }
    }
}
