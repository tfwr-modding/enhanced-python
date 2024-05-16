using System.Runtime.CompilerServices;
using HarmonyLib;

namespace EnhancedPython.Patches.Nodes;

[HarmonyDebug]
[HarmonyPatch(typeof(Node))]
public class NodePatch
{
    [HarmonyReversePatch]
    [HarmonyPatch(nameof(Node.Execute))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEnumerable<double> Execute(Node node, ProgramState state, Interpreter interpreter, int depth)
    {
        // This method is a stub and should be replaced by the actual method implementation of Node.Execute
        // This exists so that we can call base.Execute from the patched method
        throw new NotImplementedException();
    }
}
