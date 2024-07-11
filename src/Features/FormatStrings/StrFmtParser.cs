using System.Text;

namespace EnhancedPython.Features.FormatStrings;

public class StrFmtParser
{
    // TODO: patch Utils.Localizer?
    private static Dictionary<string, string> language = new Dictionary<string, string>()
    {
        { "error_fstring_empty_not_allowed", "f-string: empty expression not allowed" },
        { "error_fstring_expecting_close", "f-string: expecting '}'" },
        { "error_fstring_single_close", "f-string: single '}' is not allowed" },
        { "error_fstring_backslash_in_expression", "f-string expression part cannot include a backslash" },
        { "warning_fstring_invalid_escape", "f-string: invalid escape sequence '\\{0}'" },
    };

    public readonly record struct StringPart(string Content, bool IsExpression);
    
    // split on embedded expressions (e.g. {1+1})
    public static StringPart[] SplitOnExpressions(string str)
    {
        var index = -1;
        var currentChar = default(char);
        
        var insideBraces = false;
        var openBraceCount = 0;
        var wasLastCharEscape = false;
        var wasLastCharOpenBrace = false;
        var wasLastCharCloseBrace = false;
        
        var parts = new List<StringPart>();
        var currentPart = new StringBuilder();
        
        // iterate over the string
        while (MoveNext())
        {
            if (insideBraces) HandleInsideBraces();
            else HandleOutsideBraces();
        }

        // post parsing
        if (insideBraces || wasLastCharOpenBrace)
        {
            throw new ExecuteException(FormatError("error_fstring_expecting_close", currentChar));
        }

        if (wasLastCharCloseBrace)
        {
            throw new ExecuteException(FormatError("error_fstring_single_close"));
        }

        // TODO(NoonKnight): is more error checking needed?

        if (currentPart.Length > 0) // is the last node not empty?
        {
            parts.Add(new StringPart(currentPart.ToString(), false));
        }

        return parts.ToArray();
        
        void LogWarning(string key, params object[] args)
        {
            var format = language.TryGetValue(key, out var value) ? value : Localizer.Localize(key);
            var formatted = string.Format(format, args);
            Plugin.Log.LogWarning(formatted);
        }

        string FormatError(string key, params object[] args)
        {
            return language.TryGetValue(key, out var value) ? value : CodeUtilities.LocalizeAndFormat(key, args);
        }

        bool MoveNext()
        {
            if (!insideBraces)
            {
                wasLastCharEscape = !wasLastCharEscape && IsEscape();
                wasLastCharOpenBrace = !wasLastCharOpenBrace && IsOpenBrace();
                wasLastCharCloseBrace = !wasLastCharCloseBrace && IsCloseBrace();
            }

            if (++index >= str.Length) return false;

            currentChar = str[index];
            return true;
        }

        bool IsCloseBrace() => currentChar == '}';
        bool IsOpenBrace() => currentChar == '{';
        bool IsEscape() => currentChar == '\\';

        void HandleInsideBraces()
        {
            // is current character a backslash?
            if (IsEscape())
            {
                // expression cannot include a backslash
                throw new ExecuteException(FormatError("error_fstring_backslash_in_expression"));
            }

            if (IsOpenBrace())
            {
                openBraceCount++; // one more open brace
                currentPart.Append(currentChar);
                return;
            }

            if (IsCloseBrace())
            {
                // one less open brace
                openBraceCount--;

                switch (openBraceCount)
                {
                    // more open braces?
                    case > 0:
                        currentPart.Append(currentChar);
                        return;
                    // too many close braces
                    case < 0:
                        throw new ExecuteException(FormatError("error_fstring_single_close"));
                }

                // TODO: handle empty expressions with whitespace
                if (currentPart.Length == 0)
                {
                    throw new ExecuteException(FormatError("error_fstring_empty_not_allowed"));
                }

                // switch to outside braces
                insideBraces = false;
                parts.Add(new StringPart(currentPart.ToString(), true));
                currentPart = new StringBuilder();

                // clear the flag
                wasLastCharCloseBrace = false;
                // clear the current char so the flag won't be set by MoveNext()
                currentChar = default;

                // next iteration
                return;
            }

            // handle any other character
            currentPart.Append(currentChar);
        }

        void HandleOutsideBraces()
        {
            // escape the current char?
            if (wasLastCharEscape)
            {
                // TODO: handle octal escapes
                // TODO: handle hex escapes
                // TODO: handle unicode escapes
                // other escapes missing?
                Dictionary<char, string> escapes = new()
                {
                    { 'a', "\a" }, // alert (bell)
                    { 'b', "\b" }, // backspace
                    { 'f', "\f" }, // form feed
                    { 'r', "\r" }, // carriage return
                    { 'v', "\v" }, // vertical tab
                    { 'n', "\n" }, // newline
                    { 't', "\t" }, // horizontal tab
                    { '\'', "'" }, // single quote
                    { '"', "\"" }, // double quote
                    { '\\', "\\" }, // double backslash
                };
                if (escapes.TryGetValue(currentChar, out var escaped))
                {
                    currentPart.Append(escaped);
                }
                else
                {
                    currentPart.Append("\\" + currentChar);
                    LogWarning("warning_fstring_invalid_escape", currentChar);
                }

                // next iteration
                return;
            }

            // was the last character an open brace?
            if (wasLastCharOpenBrace)
            {
                // is this a 2nd open brace?
                if (IsOpenBrace())
                {
                    // the two become one, the 1st was skipped
                    currentPart.Append(currentChar);

                    // next iteration
                    return;
                }

                insideBraces = true;
                openBraceCount = 1;
                parts.Add(new StringPart(currentPart.ToString(), false));
                currentPart = new StringBuilder();

                // rehandle the current char as inside braces
                index--;

                // clear the flag
                wasLastCharOpenBrace = false;
                // clear the current char so the flag won't be set by MoveNext()
                currentChar = default;

                // next iteration
                return;
            }

            if (wasLastCharCloseBrace)
            {
                // is this a 2nd close brace?
                if (!IsCloseBrace()) throw new ExecuteException(FormatError("error_fstring_single_close", currentChar));

                // the two become one, the 1st was skipped
                currentPart.Append(currentChar);

                // next iteration
                return;
            }

            // to be skipped?
            if (IsEscape() || IsOpenBrace() || IsCloseBrace()) return;

            currentPart.Append(currentChar);
        }
    }
}