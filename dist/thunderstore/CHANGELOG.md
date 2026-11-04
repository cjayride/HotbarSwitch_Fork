# v0.5.9
- Tilde only advances one row per press (no Update/OnGUI double-toggle that flickered back to row 1)
- SeneaL hotbar is painted again in LateUpdate so SeneaL cannot overwrite the active row the same frame

# v0.5.8
- Load after SeneaL UI (soft dependency) so `HotbarView` exists before it is patched
- Retry the SeneaL hook on Start if the UI assembly was not ready in Awake
- Vanilla hotbar path is unchanged when SeneaL is not installed

# v0.5.7
- SeneaL UI hotbar now follows the active inventory row (vanilla HotkeyBar still does too)
- Keys 1-8 remap through `GetItemAt` only while the hotbar is being read, so the inventory window stays on real rows
- SeneaL extra/action/quick slots keep their own rows

# v0.5.6
- Hotkey now uses Valheim `ZInput` so `BackQuote` / `` ` `` works on 1.0
- Old string configs that stored `` ` `` no longer leave the bind as an unreadable key
- Extra-bar name filter no longer treats HUD parents named "equip" as extra bars

# v0.5.5
- Fixed hitch when cycling back to the first hotbar row
- No longer scans every HotkeyBar with `FindObjectsOfTypeAll` and rebuilds their UI on each press
- Extra quick-slot bars no longer share the row remap, so wrapping rows cannot recreate those UIs
- Main hotbar keeps its existing slots instead of destroying and recreating them when a row has fewer items

# v0.5.4
- Hotkey now uses BepInEx `KeyboardShortcut` instead of a free-text string
- Any Unity `KeyCode` works (including `Keypad2`), plus Ctrl/Alt/Shift combos
- With Configuration Manager, click the hotkey field and press a key, or pick from the key dropdown
- Existing configs that used `` ` `` keep the default BackQuote bind

# v0.5.3
- Verified against current Valheim (`valheim.exe` 6000.0.75f1, Steam build 26249040)
- Removed per-frame hotbar reflection painting and the global `GetItemAt` patch
- Keys 1-8 now read the active row directly; HUD uses vanilla `UpdateIcons` + `GetBoundItems`
- Patched `Inventory.GetHotbar` for the 1.0 API
- Hotkey is parsed once instead of every frame

# v0.5.2
- Updated for Valheim 1.0
- Hotbar switch no longer moves items, so stacks cannot be overwritten
- Keys 1-8 and the main hotbar HUD cycle inventory rows immediately
- Extra quick-slot bars (Z / V / B) keep their own icons

# v0.5.1
- Hotbar HUD icons now follow the active row on the same frame instead of waiting for a slow inventory redraw

# v0.5.0
- Hotbar switch no longer moves items. The key only changes which inventory row keys 1-8 and the hotbar HUD use, so stacks cannot be overwritten
- Updated for Valheim 1.0

# v0.4.3
- Updated for Valheim 1.0
- Faster hotbar switch: items swap immediately, and the 8 hotbar icons update without rebuilding the full inventory UI

# v0.4.2
- Faster combat swap: only redraw the player item grid, not the full inventory/crafting UI

# v0.4.1
- Faster hotbar switch: refresh the inventory UI directly instead of calling `Inventory.Changed`, which was rebuilding recipes and build pieces

# v0.4.0
- Updated for Valheim 1.0
- BepInEx dependency updated to denikson-BepInExPack_Valheim-5.4.2350
- Inventory row switch now calls the 1.0 `Changed(bool, bool)` refresh

# v0.3.0
- Removed terminal reload
- Added compatibility for v0.217.46
