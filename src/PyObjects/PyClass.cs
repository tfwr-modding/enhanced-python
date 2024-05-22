namespace EnhancedPython.PyObjects;

public class PyClass : IPyObject
{
    public string className;
    
    public PyFunction[] methods;

    public Node syntaxTree;

    public IPyObject methodObject;

    public Scope parentScope;

    public PyClass(string className, PyFunction[] methods, Scope parentScope, IPyObject methodObject = null)
    {
        foreach (var method in methods) method.methodObject = this;

        this.className = className;
        this.methods = methods;
        this.parentScope = parentScope;
        this.methodObject = methodObject;
    }

    public override string ToString()
    {
        return $"<class '{className}'>";
    }
}
