// using HarmonyLib;
//
// namespace EnhancedPython.Patches;
//
// [HarmonyPatch(typeof(TokenStream))]
// public class TokenStreamPatches
// {
//     [HarmonyPostfix]
//     [HarmonyPatch(nameof(TokenStream.Consume))]
//     public static void Consume(Token __result, ref TokenStream __instance)
//     {
//         var typeName = (__result.type >= TokenType.UNKNOWN + 4096 ? ((ExtendedTokenType)__result.type).ToString() : __result.type.ToString());
//         var nextFiveTokens = __instance.tokens.Take(5).Select(t => t.type >= TokenType.UNKNOWN + 4096 ? ((ExtendedTokenType)t.type).ToString() : t.type.ToString()).ToArray();
//
//         Plugin.Log.LogInfo($"Consumed {typeName}: {__result?.value} | Next five tokens: {string.Join(", ", nextFiveTokens)}");
//     }
// }
