using System.Globalization;
using EnhancedPython.utils;
using HarmonyLib;

namespace EnhancedPython.Patches;

[HarmonyPatch(typeof(Scope))]
public class ScopePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Scope.Evaluate))]
    public static bool Evaluate(string s,
        Scope __instance, ref IPyObject __result)
    {
        var pyObject = Scope.EvaluateConstant(s);
        if (pyObject != null)
        {
            __result = pyObject;
            goto end;
        }
        
        if (s.StartsWith("\"") || s.StartsWith("'"))
        {
            __result = new PyString(s.Substring(1, s.Length - 2));
            goto end;
        }
        
        if (double.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
        {
            __result = new PyNumber(result);
            goto end;
        }
        
        if (__instance.vars.TryGetValue(s, out __result)) goto end;
        
        Plugin.Log.LogInfo($"Unknown node: {s}, {string.Join(",", __instance.vars.Keys)}");

        if (__instance.parentScope == null)
            throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_name_not_defined", s));
        
        __result = __instance.parentScope.Evaluate(s);

        end:
        return PrefixAction.SkipOriginal;
    }
}
