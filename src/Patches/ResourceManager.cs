using EnhancedPython.Options;
using EnhancedPython.utils;
using HarmonyLib;
using UnityEngine;

namespace EnhancedPython;

[HarmonyPatch(typeof(ResourceManager))]
public class ResourceManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ResourceManager.GetAllOptions))]
    public static void GetAllOptionsPostfix(ref OptionSO[] __result)
    {
        __result = Singleton<OptionsManager>.Instance.UpdateOptions(__result);
    }
}
