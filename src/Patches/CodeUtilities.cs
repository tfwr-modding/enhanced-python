using System.Text.RegularExpressions;
using HarmonyLib;

namespace EnhancedPython.Patches;

public static class CodeUtilitiesPatch
{
    private static List<(Regex, string)> colors = new()
    {
        (new Regex("""\b(class)\b"""), "#e3a63d"),
    };
    
    public static void Apply()
    {
        CodeUtilities.colors.InsertRange(0, colors);
    }
}
