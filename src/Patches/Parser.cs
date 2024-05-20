using EnhancedPython.Patches.Nodes;
using EnhancedPython.utils;
using HarmonyLib;

namespace EnhancedPython.Patches;

[HarmonyPatch(typeof(Parser))]
public class ParserPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Parser.Statement))]
    public static bool Statement(
        // Parameters
        TokenStream stream, CodeWindow f, int indentation, bool global,
        // Injected
        ref Node __result
    ) {
        var type = (uint?)stream.Current?.type;
        if (type == null) return PrefixAction.CallOriginal;

        switch (type)
        {
            case (uint)ExtendedTokenType.CLASS_KEYWORD:
            {
                __result = ClassNode.Parse(stream, f, indentation, global);
                return PrefixAction.SkipOriginal;
            }
        }

        return PrefixAction.CallOriginal;
    }
    
    internal static List<(List<TokenType>, Func<TokenStream, CodeWindow, int, bool, Node>)> operators = new()
    {
        // Bitwise operators
        {(new() { (TokenType)ExtendedTokenType.BITWISE_SHIFT_LEFT }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_SHIFT_RIGHT }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_NOT }, Parser.UnaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_AND }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_XOR }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_OR }, Parser.BinaryExpression)},

        // f"string" and r"string"
        {(new() { (TokenType)ExtendedTokenType.STRING_LITERAL_PREFIX }, Parser.UnaryExpression)},
    };
    
    public static void Apply()
    {
        Parser.operators.InsertRange(0, operators);
    }
}
