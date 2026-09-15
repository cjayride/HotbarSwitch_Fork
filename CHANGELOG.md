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
