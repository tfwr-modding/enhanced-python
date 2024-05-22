using EnhancedPython.PyObjects;
using HarmonyLib;

namespace EnhancedPython.Patches;

[HarmonyPatch(typeof(Interpreter))]
public class InterpreterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Interpreter.CallFunction))]
    static IEnumerable<double> CallFunction(
        IEnumerable<double> result,
        // Arguments
        PyFunction func, List<IPyObject> parameters,
        int wordStart, int wordEnd, int depth
    ) {
        Plugin.Log.LogInfo($"aaaa {func.methodObject}");
        if (func.methodObject is not PyClass pyClass)
        {
            foreach (var item in result) yield return item;
            yield break;
        }
        
        Plugin.Log.LogInfo($"Calling a class method! {pyClass.className}.{func.functionName}");
        throw new ExecuteException("NotImplemented", wordStart, wordEnd);
    }
}
