using HarmonyLib;

namespace EnhancedPython;

[HarmonyPatch(typeof(CycleOptionUI))]
public class CycleOptionUIPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CycleOptionUI.Clicked))]
    public static void ClickedPostfix(CycleOptionUI __instance)
    {
        // Bug fix: Update the value of the option after it has been clicked
        __instance.UpdateValue();
    }
}
