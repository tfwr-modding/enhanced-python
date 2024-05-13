using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EnhancedPython;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;
    
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    
    private void Awake()
    {
        Instance = this;
        Log = Logger;
        
        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        TokenizerPatcher.Init();
        ParserPatcher.Init();
        harmony.PatchAll();
    }
}
