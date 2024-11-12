using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class OnCrimeWitnessPatch
{
    private const string OnCrimeWitnessClosure = $"<{nameof(AI_Steal.Run)}>b__4";
    private static Type? _closures;

    private static string CaughtPrompt => Lang.langCode switch {
        "CN" => "你被抓了现行！",
        "EN" => "You were caught in the act!",
        "JP" => "目撃されました！",
        _ => "",
    };

    internal static MethodInfo TargetMethod()
    {
        _closures = AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass"));
        return AccessTools.Method(
            _closures,
            OnCrimeWitnessClosure
        );
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Cgt),
                new CodeMatch(OpCodes.Ret))
            .Advance(3)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(_closures, "chara")),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(OnCrimeWitnessPatch), nameof(ModKarmaOnCaught))))
            .InstructionEnumeration();
    }

    private static void ModKarmaOnCaught(bool isCrime, Chara? target)
    {
        if (!isCrime) {
            return;
        }

        EClass.pc.Say(CaughtPrompt);

        if (target != null && (target.IsPCFaction || target.OriginalHostility >= Hostility.Friend)) {
            EClass.player.ModKarma(-1);
        } else if (target == null || target.hostility > Hostility.Enemy) {
            EClass.player.ModKarma(-1);
        }
    }
}