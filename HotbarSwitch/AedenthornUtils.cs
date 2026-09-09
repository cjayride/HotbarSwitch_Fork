using UnityEngine;

public class AedenthornUtils
{
    public static bool IgnoreKeyPresses(bool extra = false)
    {
        if (!extra)
            return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || (Chat.instance != null && Chat.instance.HasFocus());
        return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || (Chat.instance != null && Chat.instance.HasFocus()) || StoreGui.IsVisible() || InventoryGui.IsVisible() || Menu.IsVisible() || (TextViewer.instance != null && TextViewer.instance.IsVisible());
    }
    public static bool CheckKeyDown(string value)
    {
        try
        {
            return Input.GetKeyDown(value.ToLower());
        }
        catch
        {
            return false;
        }
    }
}
