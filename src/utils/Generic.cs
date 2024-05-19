using System.Diagnostics.CodeAnalysis;

namespace EnhancedPython.utils;

public static class Generic
{
    public static bool IsNull<T>([NotNullWhen(false)] this T obj)
    {
        return obj == null;
    }
    
    public static bool IsNotNull<T>([NotNullWhen(true)] this T obj)
    {
        return obj != null;
    }
}
