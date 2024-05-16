using HarmonyLib;

namespace EnhancedPython.Patches.Nodes;

[HarmonyPatch(typeof(UnaryExprNode))]
public class UnaryExprNodePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnaryExprNode.Execute))]
    static IEnumerable<double> Execute(
        IEnumerable<double> result,
        // Arguments
        ProgramState state, Interpreter interpreter, int depth,
        // Injected arguments
        UnaryExprNode __instance)
    {
        var op = __instance.op;
        
        var customOPs = new[] { "~" };
        if (!customOPs.Contains(op))
        {
            foreach (var item in result) yield return item;
            yield break;
        }

        var baseEnumerator = NodePatch.Execute(__instance, state, interpreter, depth);;
        foreach (var item in baseEnumerator) yield return item;
        
        foreach (var cost in __instance.slots[0].Execute(state, interpreter, depth + 1)) yield return cost;
        var rhs = (IPyObject)state.returnValue;
        
        if (rhs is not PyNumber number)
            throw new ExecuteException(CodeUtilities.FormatError("error_bad_unary_operator", op, rhs), __instance.wordStart, __instance.wordEnd);
        
        // Source: https://stackoverflow.com/a/2751597
        if (Math.Abs(number.num % 1) >= (double.Epsilon * 100))
        {
            // number is not an integer
            throw new ExecuteException(CodeUtilities.FormatError("error_bad_unary_operator", op, rhs), __instance.wordStart, __instance.wordEnd);
        }
        
        switch (op)
        {
            case "~":
            {
                long num = Convert.ToInt32(number.num);
                state.returnValue = new PyNumber(~num);

                yield return 1.0;
                yield break;
            }
        }
    }
}
