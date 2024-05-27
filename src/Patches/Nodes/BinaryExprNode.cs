using HarmonyLib;

namespace EnhancedPython.Patches.Nodes;

[HarmonyPatch(typeof(BinaryExprNode))]
public class BinaryExprNodePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BinaryExprNode.Execute))]
    static IEnumerable<double> Execute(
        // This is a pass-through postfix,
        // the first argument is the returned value of the original method.
        IEnumerable<double> result,
        // Arguments
        ProgramState state, Interpreter interpreter, int depth,
        // Injected arguments
        UnaryExprNode __instance
    ) {
        var op = __instance.op;
        
        // check if the operator is a custom one
        // if not, return the original result
        var customOPs = new[] { "^", "&", "|", "<<", ">>" };
        if (!customOPs.Contains(op))
        {
            foreach (var item in result) yield return item;
            yield break;
        }

        // prepare the program state
        var baseEnumerator = NodePatch.Execute(__instance, state, interpreter, depth);;
        foreach (var item in baseEnumerator) yield return item;
        
        // evaluate left-hand side
        foreach (var cost in __instance.slots[0].Execute(state, interpreter, depth + 1)) yield return cost;
        var lhs = state.returnValue;
        
        // evaluate right-hand side
        foreach (var cost in __instance.slots[1].Execute(state, interpreter, depth + 1)) yield return cost;
        var rhs = state.returnValue;

        // check if both sides are numbers
        if (lhs is not PyNumber leftHandSide ||
            rhs is not PyNumber rightHandSide
        ) {
            throw new ExecuteException(CodeUtilities.FormatError("error_bad_bin_operator", op, lhs, rhs));
        }
        
        // check if both sides are integers
        // Source: https://stackoverflow.com/a/2751597
        if (
            Math.Abs(leftHandSide.num % 1) >= (double.Epsilon * 100) &&
            Math.Abs(rightHandSide.num % 1) >= (double.Epsilon * 100)
        ) {
            throw new ExecuteException(CodeUtilities.FormatError("error_bad_bin_operator", op, lhs, rhs));
        }
        
        var (iLhs, iRhs) = (Convert.ToInt32(leftHandSide.num), Convert.ToInt32(rightHandSide.num));
        state.returnValue = new PyNumber(op switch
        {
            "^" => iLhs ^ iRhs,
            "&" => iLhs & iRhs,
            "|" => iLhs | iRhs,
            "<<" => iLhs << iRhs,
            ">>" => iLhs >> iRhs,
            _ => throw new NotImplementedException()
        });
        
        yield return 1.0;
    }
}
