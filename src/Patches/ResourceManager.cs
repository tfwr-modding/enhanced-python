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
                var bf = Saver.Inst.mainFarm.workspace.interpreter.bf;
                bf.CorrectParams(parameters, new List<Type> { typeof(UnlockSO) }, name);
                var unlockSO = parameters[0] as UnlockSO;
                var parentUnlock = ResourceManager.GetUnlock(unlockSO.parentUnlock);
                bf.interp.State.ReturnValue = parentUnlock;
                return bf.interp.GetOpCount(NodeType.Expr);
            };
        }),
/* in game test code
quick_print('len(list(Entities)) =>', len(list(Entities)))
i = 0
for e in Entities:
    quick_print(i, e)
    quick_print('  get_yield() =>', get_yield(e))
    if get_cost(e) != {}:
        quick_print('  get_cost() =>', get_cost(e))
    i += 1
*/
        BuiltinFunction.Create("get_yield", "senses", name =>
        {
            return parameters =>
            {
                var bf = Saver.Inst.mainFarm.workspace.interpreter.bf;
                bf.CorrectParams(parameters, new List<Type> { typeof(FarmObject) }, name);
                var farmObject = parameters[0] as FarmObject;
                if (farmObject is not Growable growable)
                {
                    var entity = ResourceManager.GetEntity(farmObject.objectName);
                    if (entity is null)
                        throw new ExecuteException(CodeUtilities.FormatError("error_wrong_args", 1, name + "()", parameters[0]));
                    bf.interp.State.ReturnValue = new PyNone();
                    return bf.interp.GetOpCount(NodeType.Expr);
                }
                var farm = Saver.Inst.mainFarm;
                var yieldUpgradeName = growable.yieldUpgradeName;
                var numFreeUpgrades = growable.numFreeUpgrades;
                var numUpgrades = farm.GetNumUpgrades(yieldUpgradeName);
                var yieldFactor = Math.Max(numUpgrades+numFreeUpgrades,1) * Saver.Inst.harvestFactor;
                // hmm, too bad this is needed
                var harvestItems = farmObject.objectName == "dinosaur" ? new ItemBlock("bones", 1) : growable.harvestItems;
                bf.interp.State.ReturnValue = BuiltinFunctions.ItemsToNewDict(harvestItems * yieldFactor);
                return bf.interp.GetOpCount(NodeType.Expr);
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
