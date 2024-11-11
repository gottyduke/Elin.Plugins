using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class OnCrimeWitnessPatch
{
    private const string OnCrimeWitnessClosure = $"<{nameof(AI_Steal.Run)}>b__4";

    private static string CaughtPrompt => Lang.langCode switch {
        "CN" => "你被抓了现行！",
        "EN" => "You were caught in the act!",
        "JP" => "目撃されました！",
        _ => "",
    };

    internal static MethodInfo TargetMethod()
    {
        var closures = AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass"));
        return AccessTools.Method(
            closures,
            OnCrimeWitnessClosure
        );
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count; ++i) {
            if (i >= 4 &&
                codes[i].opcode == OpCodes.Ret &&
                codes[i - 1].opcode == OpCodes.Cgt &&
                codes[i - 2].opcode == OpCodes.Add &&
                codes[i - 3].opcode == OpCodes.Ldloc_0) {
                codes[i].labels.Add(generator.DefineLabel());
                var disp = codes[i].labels[0];

                yield return new(OpCodes.Dup);
                yield return new(OpCodes.Brfalse_S, disp);
                yield return new(OpCodes.Call,
                    AccessTools.Method(typeof(OnCrimeWitnessPatch), nameof(ModKarmaOnCaught)));
            }

            yield return codes[i];
        }
    }

    private static void ModKarmaOnCaught()
    {
        EClass.pc.Say(CaughtPrompt);
        EClass.player.ModKarma(-1);
    }
}