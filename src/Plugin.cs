using BepInEx;
using BepInEx.Logging;
using EnhancedPython.Patches;
using HarmonyLib;

namespace EnhancedPython;

[BepInDependency("FarmerLib", BepInDependency.DependencyFlags.HardDependency)]
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
        
        TokenizerPatches.Apply();
        ParserPatches.Apply();
        CodeUtilitiesPatch.Apply();
        harmony.PatchAll();
        
        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}
