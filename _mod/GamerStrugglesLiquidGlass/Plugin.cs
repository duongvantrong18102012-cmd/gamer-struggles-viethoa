using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace GamerStrugglesLiquidGlass;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    internal static Harmony Harmony;

    public override void Load()
    {
        Log = base.Log;
        Settings.Load();

        try
        {
            Harmony = new Harmony(PluginInfo.Guid);
            Harmony.PatchAll(typeof(Patches));
            Log.LogInfo("Harmony patches applied.");
        }
        catch (System.Exception ex)
        {
            Log.LogError("Harmony patch setup failed: " + ex);
        }

        AddComponent<ModMenu>();
        Log.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
    }

    public override bool Unload()
    {
        Settings.Save();

        try
        {
            if (ModMenu.Instance != null)
                ModMenu.Instance.RestoreGameState();
        }
        catch (System.Exception ex)
        {
            Log?.LogWarning("Restore on unload failed: " + ex.Message);
        }

        try
        {
            Harmony?.UnpatchSelf();
        }
        catch (System.Exception ex)
        {
            Log?.LogWarning("Unpatch failed: " + ex.Message);
        }

        Time.timeScale = 1f;
        return true;
    }
}

public static class PluginInfo
{
    public const string Guid = "com.duong.gamerstruggles.liquidglass";
    public const string Name = "Gamer Struggles Liquid Glass";
    public const string Version = "1.0.0";
}
