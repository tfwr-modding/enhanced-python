using System.Text;
using EnhancedPython.utils;
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

        var customOPs = new[]
        {
			// bitwise not
			"~",
			// f"string" and r"string"
			"f", "r"
        };

        if (!customOPs.Contains(op))
        {
            foreach (var item in result) yield return item;
            yield break;
        }

        var baseEnumerator = NodePatch.Execute(__instance, state, interpreter, depth);
        foreach (var item in baseEnumerator) yield return item;

        foreach (var cost in __instance.slots[0].Execute(state, interpreter, depth + 1)) yield return cost;
        var rhs = state.returnValue;

        switch (op)
        {
            case "~":
            {
                if (rhs is not PyNumber number)
                    throw new ExecuteException(CodeUtilities.FormatError("error_bad_unary_operator", op, rhs));

                // Source: https://stackoverflow.com/a/2751597
                if (Math.Abs(number.num % 1) >= (double.Epsilon * 100))
                {
                    // number is not an integer
                    throw new ExecuteException(CodeUtilities.FormatError("error_bad_unary_operator", op, rhs));
                }

                var num = Convert.ToInt64(number.num);
                state.returnValue = new PyNumber(~num);
                break;
            }

            case "f":
            {
                if (rhs is not PyString pyString)
                    throw new ExecuteException(CodeUtilities.FormatError("error_bad_unary_operator", op, rhs));

                // Step 1: Extract all the {} expressions from the string
                var parts = Features.FormatStrings.StrFmtParser.SplitOnExpressions(pyString.str);
                Plugin.Log.LogInfo($"SplitOnEmbeddedExpressions():\n • {string.Join("\n • ", parts)}");

                // If there are no expressions, return the string as is.
                if (!parts.Any(p => p.IsExpression))
                {
                    state.returnValue = new PyString(pyString.str);
                    break;
                }
                
                // Step 2: Evaluate the expressions
                var sb = new StringBuilder();
                foreach (var part in parts)
                {
                    if (part.IsExpression)
                    {
                        // TODO(Arjix): Interpreter.Evaluate is not a real thing :(
                        // sb.Append(interpreter.Evaluate(part.Content, state, depth + 1));
                    }
                    else sb.Append(part.Content);
                }

                // Step 3: Create a new string with the evaluated expressions.
                state.returnValue = new PyString(sb.ToString());
                break;
            }

            case "r": throw new NotImplementedException("Raw strings are not implemented yet.");
        }
        yield return 1.0; // number of ops
    }
}
