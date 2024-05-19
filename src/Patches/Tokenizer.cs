using System.Text.RegularExpressions;

namespace EnhancedPython.Patches;


public enum ExtendedTokenType
{
    BITWISE_OR            = (TokenType.UNKNOWN + 4096) + 0,
    BITWISE_XOR           = (TokenType.UNKNOWN + 4096) + 1,
    BITWISE_AND           = (TokenType.UNKNOWN + 4096) + 2,
    BITWISE_NOT           = (TokenType.UNKNOWN + 4096) + 3,
    BITWISE_SHIFT_LEFT    = (TokenType.UNKNOWN + 4096) + 4,
    BITWISE_SHIFT_RIGHT   = (TokenType.UNKNOWN + 4096) + 5,
    
    STRING_LITERAL_PREFIX = (TokenType.UNKNOWN + 4096) + 6,
}

public static class TokenizerPatches
{
    public static List<(Regex, uint)> regexes = new()
    {
        // define invalid tokens
        (new Regex("""^(?:&|\||\^|~){2,}"""), (uint)TokenType.UNKNOWN),
        
        // define bitwise operators
        (new Regex("""^\|"""), (uint)ExtendedTokenType.BITWISE_OR),
        (new Regex("""^\^"""), (uint)ExtendedTokenType.BITWISE_XOR),
        (new Regex("""^\&"""), (uint)ExtendedTokenType.BITWISE_AND),
        (new Regex("^~"), (uint)ExtendedTokenType.BITWISE_NOT),
        (new Regex("^<<"), (uint)ExtendedTokenType.BITWISE_SHIFT_LEFT),
        (new Regex("^>>"), (uint)ExtendedTokenType.BITWISE_SHIFT_RIGHT),
        
        // define string literals
        (new Regex("""^(['\"])\1"""), (uint)TokenType.STRING),                            // Empty string
        (new Regex("""^(['\"])(.*?[^\\])\1"""), (uint)TokenType.STRING),                  // Normal string
        (new Regex("^[fr](?='.*?')"), (uint)ExtendedTokenType.STRING_LITERAL_PREFIX),     // f'string' and r'string'
        (new Regex("""^[fr](?=".*?")"""), (uint)ExtendedTokenType.STRING_LITERAL_PREFIX), // f"string" and r"string"
    };
    
    public static void Apply()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        Tokenizer.regexes.InsertRange(0, regexes.Select(tuple => (tuple.Item1, (TokenType)tuple.Item2)));
    }
}
