namespace EnhancedPython.Patches;

public class ParserPatches
{
    public static List<(List<TokenType>, Func<TokenStream, CodeWindow, int, bool, Node>)> operators = new()
    {
        {(new() { (TokenType)ExtendedTokenType.BITWISE_SHIFT_LEFT }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_SHIFT_RIGHT }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_NOT }, Parser.UnaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_AND }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_XOR }, Parser.BinaryExpression)},
        {(new() { (TokenType)ExtendedTokenType.BITWISE_OR }, Parser.BinaryExpression)},
    };
    
    public static void Apply()
    {
        Parser.operators.InsertRange(0, operators);
    }
}
