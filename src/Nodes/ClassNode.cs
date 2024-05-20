namespace EnhancedPython.Patches.Nodes;

public class ClassNode: Node
{
    public static Dictionary<string, ClassNode> parsedClasses = new();
    
    public string className;
    public string? parentName;
    public List<DefNode> methods;
    
    public ClassNode(
        string className, string? parentName,
        List<DefNode> methods,
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
        foreach (var item in base.Execute(state, interpreter, depth)) yield return item;
        Plugin.Log.LogInfo("ClassNode.Execute() called!");
    }

    public static ClassNode Parse(TokenStream stream, CodeWindow f, int indentation, bool global)
    {
        _ = stream.Consume((TokenType)ExtendedTokenType.CLASS_KEYWORD);
        var name = stream.Consume(TokenType.IDENTIFIER);
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
        
        var methods = new List<DefNode>();
        // TODO: Parse methods and attributes, based on the indentation
        
        return new ClassNode(name.value, baseClass?.value, methods, global, f, name.startIndex, name.startIndex);
    }
}
