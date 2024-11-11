using System.Collections.Generic;
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
    internal static IEnumerable<CodeInstruction> OnModKarmaIl(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);
        Label? disp = null;
        for (var i = codes.Count - 4; i > 0; --i) {
            if (codes[i].opcode != OpCodes.Callvirt ||
                !codes[i].operand.ToString().Contains(nameof(Player.ModKarma))) {
                continue;
            }

            codes[i + 1].labels.Add(generator.DefineLabel());
            disp = codes[i + 1].labels[0];
            break;
        }

        var patched = false;
        for (var i = 0; i < codes.Count; ++i) {
            if (i <= codes.Count - 4 &&
                disp is not null &&
                !patched &&
                codes[i].opcode == OpCodes.Ldarg_0 &&
                codes[i + 1].opcode == OpCodes.Ldfld &&
                codes[i + 1].operand.ToString().Contains("chara") &&
                codes[i + 2].opcode == OpCodes.Brfalse) {
                yield return new(OpCodes.Br_S, disp);
                patched = true;
            }

            yield return codes[i];
        }
    }
}