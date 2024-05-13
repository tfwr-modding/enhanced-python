using EnhancedPython.Nodes;

namespace EnhancedPython;

public class ParserPatcher
{
    public static List<(List<TokenType>, Func<TokenStream, CodeWindow, int, bool, Node>)> operators = new()
    {
        {( new()
           {
               (TokenType)ExtendedTokenType.BITWISE_AND,
               (TokenType)ExtendedTokenType.BITWISE_OR,
               (TokenType)ExtendedTokenType.BITWISE_XOR,
           },
           (stream, window, opIndex, canLineBreak) => {
               if (canLineBreak) Parser.LineBreaks(stream);
               
               var node = Parser.Expression(stream, window, opIndex + 1, canLineBreak);
               if (canLineBreak) Parser.LineBreaks(stream);
               
               foreach (var tokenType in Parser.operators[opIndex].Item1)
               {
                   var token = stream.Current;
                   if (token == null || token.type != tokenType) continue;
                   
                   var op = stream.Consume(tokenType, null);
                   var node2 = Parser.operators[opIndex].Item2(stream, window, opIndex, canLineBreak);
                   var node3 = new BitwiseOperation(op.value, op.type, window, op.startIndex, op.startIndex + op.value.Length);
                   
                   node3.slots.Add(node);
                   if (!new Type[] { typeof(BinaryExprNode), typeof(BitwiseOperation) }.Contains(node2.GetType()) || 
                       !Parser.operators[opIndex].Item1.Contains(((BitwiseOperation)node2).opTokenType))
                   {
                       node3.slots.Add(node2);
                       return node3;
                   }
                   
                   while (node2.slots[0] is BitwiseOperation &&
                          Parser.operators[opIndex].Item1.Contains(((BitwiseOperation)node2.slots[0]).opTokenType))
                   {
                       node2 = node2.slots[0];
                   }
                   
                   node3.slots.Add(node2.slots[0]);
                   node2.slots[0] = node3;
                   
                   return node2;
               }
               return node;
           })
        }
    };
    
    public static void Init()
    {
        Parser.operators.AddRange(operators);
    }
}
