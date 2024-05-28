using System.Text;
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

                long num = Convert.ToInt64(number.num);
                state.returnValue = new PyNumber(~num);
                break;
            }

            case "f":
            {
                if (rhs is not PyString pyString)
                    throw new ExecuteException(CodeUtilities.FormatError("error_bad_unary_operator", op, rhs));

                // TODO(Step 1): Extract all the {} expressions from the string
                // Example: f"1 + 1 = {1+1}" => ["{1+1}"]
                var strings = SplitOnEmbeddedExpressions(pyString.str);
                var prefix = "\n • ";
                Plugin.Log.LogInfo($"{nameof(SplitOnEmbeddedExpressions)}():{prefix}{string.Join(prefix, strings)}");

                // TODO(Step 2): Evaluate each expression.
                // Example: ["{1+1}"] => ["2"]

                // TODO(Step 3): Create a new string with the evaluated expressions.
                // Example: f"1 + 1  = {1+1}" => "1 + 1 = 2"

                state.returnValue = new PyString(string.Join(string.Empty, strings));
                break;
            }

            case "r": throw new NotImplementedException("Raw strings are not implemented yet.");
        }
        yield return 1.0; // number of ops


        // split on embedded expressions (e.g. {1+1})
        IEnumerable<string> SplitOnEmbeddedExpressions(string str)
        {
            var index = -1;
            var insideBraces = false;
            var wasLastCharEscape = false;
            var wasLastCharOpenBrace = false;
            var wasLastCharCloseBrace = false;
            var currentChar = default(char);
            var openBraceCount = 0;
            var sbNode = new StringBuilder();
            var sbNodes = new List<StringBuilder>();

            bool IsEscape() => currentChar is '\\';
            bool IsOpenBrace() => currentChar is '{';
            bool IsCloseBrace() => currentChar is '}';
            bool MoveNext()
            {
                if (insideBraces is false)
                {
                    wasLastCharEscape = wasLastCharEscape is false && IsEscape();
                    wasLastCharOpenBrace = wasLastCharOpenBrace is false && IsOpenBrace();
                    wasLastCharCloseBrace = wasLastCharCloseBrace is false && IsCloseBrace();
                }
                if (++index < str.Length)
                {
                    currentChar = str[index];
                    return true;
                }
                return false;
            }

            // TODO: patch Utils.Localizer?
            var language = new Dictionary<string, string>()
            {
                {"error_fstring_empty_not_allowed", "f-string: empty expression not allowed"},
                {"error_fstring_expecting_close", "f-string: expecting '}'"},
                {"error_fstring_single_close", "f-string: single '}' is not allowed"},
                {"error_fstring_backslash_in_expression", "f-string expression part cannot include a backslash"},
                {"warning_fstring_invalid_escape", "f-string: invalid escape sequence '\\{0}'"},
            };
            string FormatError(string key, params object[] args)
            {
                if (sbNode.Length > 0) sbNodes.Add(sbNode);
                return language.ContainsKey(key) ? language[key] : CodeUtilities.FormatError(key, args);
            };
            void LogWarning(string key, params object[] args)
            {
                var format = language.ContainsKey(key) ? language[key] : Localizer.Localize(key);
                var formatted = string.Format(format, args);
                Plugin.Log.LogWarning(formatted);
                Logger.LogWarning(formatted, state);
            }

            // iterate over the string
            while (MoveNext() is true)
            {
                switch (insideBraces)
                {
                    case true: HandleInsideBraces(); break;
                    case false: HandleOutsideBraces(); break;
                }
            }

            // post parsing

            if (insideBraces is true || wasLastCharOpenBrace is true)
            {
                throw new ExecuteException(FormatError("error_fstring_expecting_close", currentChar));
            }
            if (wasLastCharCloseBrace is true)
            {
                throw new ExecuteException(FormatError("error_fstring_single_close"));
            }

            // TODO: is more error checking needed?

            // is the last node not empty?
            if (sbNode.Length > 0)
            {
                sbNodes.Add(sbNode);
            }

            return sbNodes.Select(n => n.ToString());

            void HandleInsideBraces()
            {
                // is current character a backslash?
                if (IsEscape() is true)
                {
                    // expression cannot include a backslash
                    throw new ExecuteException(FormatError("error_fstring_backslash_in_expression"));
                }

                if (IsOpenBrace() is true)
                {
                    openBraceCount++; // one more open brace
                    sbNode.Append(currentChar);
                    return;
                }

                if (IsCloseBrace() is true)
                {

                    // one less open brace
                    openBraceCount--;

                    // more open braces?
                    if (openBraceCount > 0)
                    {
                        sbNode.Append(currentChar);
                        return;
                    }

                    // too many close braces
                    if (openBraceCount < 0)
                    {
                        throw new ExecuteException(FormatError("error_fstring_single_close"));
                    }

                    // TODO: handle empty expressions with whitespace
                    if (sbNode.Length is 0)
                    {
                        throw new ExecuteException(FormatError("error_fstring_empty_not_allowed"));
                    }

                    // switch to outside braces
                    insideBraces = false;
                    sbNodes.Add(sbNode);
                    sbNode = new StringBuilder();

                    // clear the flag
                    wasLastCharCloseBrace = false;
                    // clear the current char so the flag won't be set by MoveNext()
                    currentChar = default;

                    // next iteration
                    return;
                }

                // handle any other character
                sbNode.Append(currentChar);
            }

            void HandleOutsideBraces()
            {
                // escape the current char?
                if (wasLastCharEscape is true)
                {
                    // TODO: handle octal escapes
                    // TODO: handle hex escapes
                    // TODO: handle unicode escapes
                    // other escapes missing?
                    Dictionary<char, string> escapes = new(){
                        {'a', "\a"}, // alert (bell)
						{'b', "\b"}, // backspace
						{'f', "\f"}, // form feed
						{'r', "\r"}, // carriage return
						{'v', "\v"}, // vertical tab
						{'n', "\n"}, // newline
						{'t', "\t"}, // horizontal tab
						{'\'', "'"}, // single quote
						{'"', "\""}, // double quote
						{'\\', "\\"}, // double backslash
					};
                    if (escapes.ContainsKey(currentChar) is true)
                    {
                        sbNode.Append(escapes[currentChar]);
                    }
                    else
                    {
                        sbNode.Append("\\" + currentChar);
                        LogWarning("warning_fstring_invalid_escape", currentChar);
                    }

                    // next iteration
                    return;
                }

                // was the last character an open brace?
                if (wasLastCharOpenBrace is true)
                {

                    // is this a 2nd open brace?
                    if (IsOpenBrace() is true)
                    {
                        // the two become one, the 1st was skipped
                        sbNode.Append(currentChar);

                        // next iteration
                        return;
                    }
                    insideBraces = true;
                    openBraceCount = 1;
                    sbNodes.Add(sbNode);
                    sbNode = new StringBuilder();

                    // rehandle the current char as inside braces
                    index--;

                    // clear the flag
                    wasLastCharOpenBrace = false;
                    // clear the current char so the flag won't be set by MoveNext()
                    currentChar = default;

                    // next iteration
                    return;
                }
                if (wasLastCharCloseBrace is true)
                {
                    // is this a 2nd close brace?
                    if (IsCloseBrace() is true)
                    {
                        // the two become one, the 1st was skipped
                        sbNode.Append(currentChar);

                        // next iteration
                        return;
                    }
                    throw new ExecuteException(FormatError("error_fstring_single_close", currentChar));
                }

                // to be skipped?
                if (IsEscape() is true || IsOpenBrace() is true || IsCloseBrace() is true)
                {
                    return;
                }
                sbNode.Append(currentChar);
            }
        }
    }
}
