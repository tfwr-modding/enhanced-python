using HarmonyLib;
using EnhancedPython.utils;
using System.Text;
using System.Runtime.InteropServices;

namespace EnhancedPython.Patches;

[HarmonyPatch(typeof(ResourceManager))]
public class ResourceManagerPatches
{
    record BuiltinFunction(string name, string unlockedBy, Func<List<IPyObject>, double> function)
    {
        public static BuiltinFunction Create(string name, string unlockedBy, Func<string, Func<List<IPyObject>, double>> getBuiltinFunction)
        {
            return new BuiltinFunction(name, unlockedBy, getBuiltinFunction(name));
        }
    };
    static BuiltinFunction[] builtinFunctions = {
/* in game test code
quick_print('len(list(Unlocks)) =>',len(list(Unlocks)))
for u in Unlocks:
    quick_print('get_unlock_parent(',u,') =>',get_unlock_parent(u))
*/
        BuiltinFunction.Create("get_unlock_parent", "auto_unlock", name =>
        {
            return parameters =>
            {
                var __instance = Saver.Inst.mainFarm.workspace.interpreter.bf;
                __instance.CorrectParams(parameters, new List<Type> { typeof(UnlockSO) }, name);
                var unlockSO = parameters[0] as UnlockSO;
                var interp = __instance.interp;
                var parentUnlock = ResourceManager.GetUnlock(unlockSO.parentUnlock);
                interp.State.ReturnValue = parentUnlock;
                return interp.GetOpCount(NodeType.Expr);
            };
        }),
/* in game test code
quick_print('len(list(Entities)) =>',len(list(Entities)))
for e in Entities:
    quick_print('get_yield_factor(',e,') =>',get_yield_factor(e))
*/
        BuiltinFunction.Create("get_yield_factor", "senses", name =>
        {
            return parameters =>
            {
                var __instance = Saver.Inst.mainFarm.workspace.interpreter.bf;
                __instance.CorrectParams(parameters, new List<Type> { typeof(FarmObject) }, name);
                var interp = __instance.interp;
                var farmObject = parameters[0] as FarmObject;
                if (farmObject is not Growable growable)
                {
                    var entity = ResourceManager.GetEntity(farmObject.objectName);
                    if (entity is null)
                        throw new ExecuteException(CodeUtilities.FormatError("error_wrong_args", 1, name + "()", parameters[0]));
                    interp.State.ReturnValue = new PyNone();
                    return interp.GetOpCount(NodeType.Expr);
                }
                var farm = Saver.Inst.mainFarm;
                var yieldUpgradeName = growable.yieldUpgradeName;
                var numFreeUpgrades = growable.numFreeUpgrades;
                var numUpgrades = farm.GetNumUpgrades(yieldUpgradeName);
                interp.State.ReturnValue = new PyNumber(Math.Max(numUpgrades+numFreeUpgrades,1) * Saver.Inst.harvestFactor);
                return interp.GetOpCount(NodeType.Expr);
            };
        }),
    };
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GetAllUnlocks))]
    public static UnlockSO[] GetAllUnlocks(
        // No parameters
        // Injected
        UnlockSO[] __result
    )
    {
        foreach (var builtinFunction in builtinFunctions)
        {
            var name = builtinFunction.name;
            var unlockedBy = builtinFunction.unlockedBy; // e.g. auto_unlock
            var function = builtinFunction.function;
            var functions = Saver.Inst.mainFarm.workspace.interpreter.bf.functions;
            if (functions.ContainsKey(name) is true)
            {
                Plugin.Log.LogError($"""unable to add builtin function "{name}", it already exists!""");
                continue;
            }
            Plugin.Log.LogWarning($"""adding builtin function "{name}" to unlock "{unlockedBy}"...""");
            functions.Add(name, function);
            // list of keywords that get unlocked by a tech tree unlock
            var unlocks = __result.FirstOrDefault(unlockSO => unlockSO.unlockName == unlockedBy).unlocks;
            if (unlocks is null)
            {
                Plugin.Log.LogError($"""Unable to add builtin function "{name}" to "{unlockedBy}"!""");
            }
            // add the function name to the list of unlocks if it's not already there
            if (unlocks.FirstOrDefault(unlock => unlock == name) != default)
            {
                // Plugin.Log.LogWarning($"builtin function \"{name}\" already exists");
                continue;
            }
            unlocks.Add(name);
        }
        return __result;
    }
}
