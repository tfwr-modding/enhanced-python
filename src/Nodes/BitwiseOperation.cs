namespace EnhancedPython.Nodes;

public class BitwiseOperation: Node
{
    public BitwiseOperation(string op, TokenType opTokenType, CodeWindow codeWindow, int startIndex, int endIndex)
        : base(codeWindow, startIndex, endIndex)
    {
        this.op = op;
        this.opTokenType = opTokenType;
        this.nodeName = "binary";
    }

    public override IEnumerable<double> Execute(ProgramState state, Interpreter interpreter, int depth)
    {
        foreach (var num in base.Execute(state, interpreter, depth))
        {
            yield return num;
        }
        
        // Evaluate the left-hand side
        foreach (var num2 in slots[0].Execute(state, interpreter, depth + 1)) yield return num2;
        var lhs = state.returnValue;
        
        // Evaluate the right-hand side
        foreach (var num2 in slots[1].Execute(state, interpreter, depth + 1)) yield return num2;
        var rhs = state.returnValue;
        
        if (lhs is not double || rhs is not double)
        {
            throw new ExecuteException(
                CodeUtilities.FormatError("error_bad_bin_operator", new [] { op, lhs, rhs }),
                wordStart, wordEnd
            );
        }
        
        // ReSharper disable PossibleInvalidCastException CompareOfFloatsByEqualityOperator
        var (iLHS, iRHS) = ((long)(double)lhs, (long)(double)rhs);
        if ((double)lhs != iLHS || (double)rhs != iRHS)
        {
            throw new ExecuteException(
                CodeUtilities.FormatError("error_bad_bin_operator", new [] { op, lhs, rhs }),
                wordStart, wordEnd
            );
        }

        state.returnValue = (ExtendedTokenType)opTokenType switch
        {
            ExtendedTokenType.BITWISE_OR => iLHS | iRHS,
            ExtendedTokenType.BITWISE_AND => iLHS & iRHS,
            ExtendedTokenType.BITWISE_XOR => iLHS ^ iRHS,
        };

        yield return 1.0;
    }
    
    public override void GetDependencies(List<string> dependencies, Interpreter interpreter)
    {
        base.GetDependencies(dependencies, interpreter);
        dependencies.Add("operators");
    }
    
    public string op;
    public TokenType opTokenType;
}
