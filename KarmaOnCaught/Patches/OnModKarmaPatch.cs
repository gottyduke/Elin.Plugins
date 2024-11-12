using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class OnModKarmaPatch
{
    private const string OnProgressCompleteClosure = $"<{nameof(AI_Steal.Run)}>b__3";

    internal static MethodInfo TargetMethod()
    {
        var closures = AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass"));
        return AccessTools.Method(
            closures,
            OnProgressCompleteClosure
        );
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnModKarmaIl(IEnumerable<CodeInstruction> instructions)
    {
        const int offset = 3 + 4 + 5 + 3 + 1 + 3 + 5 + 3;
        // CodeMatcher sucks
        var c = new List<CodeInstruction>(instructions);
        for (var i = 0; i < c.Count; ++i) {
            if (i >= 3 &&
                c[i - 1].opcode == OpCodes.Callvirt &&
                c[i - 2].opcode == OpCodes.Ldnull &&
                c[i - 3].opcode == OpCodes.Ldnull &&
                (MethodInfo)c[i - 1].operand == AccessTools.Method(typeof(Card), nameof(Card.Say),
                    [typeof(string), typeof(Card), typeof(Card), typeof(string), typeof(string)])) {
                i += offset;
            }

            yield return c[i];
        }
    }
}