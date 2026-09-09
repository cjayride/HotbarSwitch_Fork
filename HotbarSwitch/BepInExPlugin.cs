using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HotbarSwitch {
    [BepInPlugin("cjayride.HotbarSwitch", "Hotbar Switch", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin {
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> rowsToSwitch;
        public static ConfigEntry<string> hotKey;

        private void Awake() {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            rowsToSwitch = Config.Bind<int>("General", "RowsToSwitch", 2, "Rows of inventory to switch via hotkey");
            hotKey = Config.Bind<string>("General", "HotKey", "`", "Hotkey to initiate switch. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");

            if (!modEnabled.Value)
                return;
        }
        private void Update() {
            if (modEnabled.Value && !AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(hotKey.Value)) {
                Inventory inventory = Player.m_localPlayer.GetInventory();
                int gridHeight = inventory.GetHeight();
                int rows = Math.Max(1, Math.Min(gridHeight, rowsToSwitch.Value));

                List<ItemDrop.ItemData> items = Traverse.Create(inventory).Field("m_inventory").GetValue<List<ItemDrop.ItemData>>();
                for (int i = 0; i < items.Count; i++) {
                    if (items[i].m_gridPos.y >= rows)
                        continue;
                    items[i].m_gridPos.y--;
                    if (items[i].m_gridPos.y < 0)
                        items[i].m_gridPos.y = rows - 1;
                }
                Traverse.Create(inventory).Method("Changed", new Type[] { typeof(bool), typeof(bool) }).GetValue(false, false);
            }
        }
    }
}
