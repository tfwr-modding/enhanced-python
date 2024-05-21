using EnhancedPython.PyObjects;
using EnhancedPython.utils;

namespace EnhancedPython.Patches.Nodes;

public class ClassNode: Node
{
    public static Dictionary<string, ClassNode> parsedClasses = new();
    
    public string className;
    public string? parentName;
    public DefNode[] methods;
    
    public ClassNode(
        string className, string? parentName,
        DefNode[] methods,
        bool global,
        CodeWindow codeWindow, int startIndex, int endIndex
    ) : base(codeWindow, startIndex, endIndex)
    {
        nodeName = "class";

        this.className = className;
        this.parentName = parentName;
        this.methods = methods;
        
        if (global) parsedClasses[className] = this;
    }

    public override IEnumerable<double> Execute(ProgramState state, Interpreter interpreter, int depth)
    {
        foreach (var item in base.Execute(state, interpreter, depth + 1)) yield return item;
        Blink(interpreter);

        var scope = new Scope(className, interpreter.State.CurrentScope);
        var funcs = methods.Select(method => new PyFunction(method.funcName, method.slots[0], scope)).ToArray();

        interpreter.State.CurrentScope.SetVar(className, new PyClass(className, funcs, interpreter.State.CurrentScope));
        state.ReturnValue = new PyNone();
        
        yield return interpreter.GetOpCount(NodeType.Expr);
    }

    public static ClassNode Parse(TokenStream stream, CodeWindow f, int indentation, bool global)
    {
        _ = stream.Consume((TokenType)ExtendedTokenType.CLASS_KEYWORD);
        var className = stream.Consume(TokenType.IDENTIFIER);
        Token? baseClass = null;
        
        if ((uint?)stream.Current?.type == (uint)TokenType.BRACKET_OPEN)
        {
            stream.Consume(TokenType.BRACKET_OPEN);
            try {
                baseClass = stream.Consume(TokenType.IDENTIFIER);
            } catch (ParseException p) {
                // ignore, python allows empty brackets
            }
            stream.Consume(TokenType.BRACKET_CLOSE);
        }
        
        _ = stream.Consume(TokenType.COLON, "error_missing_colon");

        var block = Parser.Block(stream, f, indentation) as SequenceNode;
        var seq = block!.slots;

        var methods = seq.OfType<DefNode>().ToArray();
        var rest = seq.Except(methods).ToArray();

        foreach (var item in rest)
        {
            if (item is not { nodeName: "assign" })
                throw new ParseException("error_unexpected_node", item.wordStart, item.wordEnd);
        }

        return new ClassNode(className.value, baseClass?.value, methods, global, f, className.startIndex, className.startIndex);
    }
}
