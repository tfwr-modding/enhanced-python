using System.Text.RegularExpressions;

namespace EnhancedPython;

public enum ExtendedTokenType
{
    BITWISE_OR = TokenType.UNKNOWN + 4096,
    BITWISE_AND = TokenType.UNKNOWN + 4097,
    BITWISE_XOR = TokenType.UNKNOWN + 4098,
}

public static class TokenizerPatcher
{
    public static List<(Regex, uint)> regexes = new()
    {
        (new Regex("""^(?:&|\||\^){2,}"""), (uint)TokenType.UNKNOWN),
        (new Regex("""^\|"""), (uint)ExtendedTokenType.BITWISE_OR),
        (new Regex("""^\^"""), (uint)ExtendedTokenType.BITWISE_XOR),
        (new Regex("""^\&"""), (uint)ExtendedTokenType.BITWISE_AND),
    };
    
    public static void Init()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        Tokenizer.regexes.InsertRange(0, regexes.Select(tuple => (tuple.Item1, (TokenType)tuple.Item2)));
    }
}
