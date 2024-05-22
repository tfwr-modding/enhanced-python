// using HarmonyLib;
//
// namespace EnhancedPython.Patches.Nodes;
//
// [HarmonyPatch(typeof(CallNode))]
// public class CallNodePatches
// {
//     [HarmonyPostfix]
//     [HarmonyPatch(nameof(CallNode.Execute))]
//     public static IEnumerable<double> Execute(
//         IEnumerable<double> result,
//         // Arguments
//         ProgramState state, Interpreter interpreter, int depth,
//         // Injected arguments
//         CallNode __instance
//     ) {
//         var slots = __instance.slots;
//         var wordStart = __instance.wordStart;
//         var wordEnd = __instance.wordEnd;
//         
//         var baseEnumerator = NodePatch.Execute(__instance, state, interpreter, depth);;
//         foreach (var item in baseEnumerator) yield return item;
//         
//         foreach (var item in slots[0].Execute(state, interpreter, depth + 1)) yield return item;
//         if (state.ReturnValue is not PyFunction pyFunc) throw new ExecuteException("this is not a function", wordStart, wordEnd);
//         
//         foreach (var item in slots[1].Execute(state, interpreter, depth + 1)) yield return item;
//         if (state.ReturnValue is not InternalPySequence pySequence) throw new ExecuteException("this is not a sequence", wordStart, wordEnd);
//         
//         var elements = pySequence.elements;
//         __instance.Blink(interpreter);
//         
//         foreach (var item in interpreter.CallFunction(pyFunc, elements, wordStart, wordEnd, depth + 1)) yield return item;
//     }
// }
