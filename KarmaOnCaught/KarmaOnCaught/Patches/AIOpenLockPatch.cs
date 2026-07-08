using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EModding.Helper;
using HarmonyLib;

namespace KarmaOnCaught.Patches;

[HarmonyPatch]
internal class AIOpenLockPatch
{
    private static KocConfig.Patch Config => KocConfig.Managed["Lockpick"];

    internal static bool Prepare()
    {
        return Config.Enabled!.Value;
    }

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(AI_OpenLock), "<CreateProgress>b__8_1");
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchEndBackwards(
                new OperandContains(OpCodes.Callvirt, nameof(Point.TryWitnessCrime)))
            .EnsureValid("pos.TryWitnessOpenLock")
            .SetInstruction(
                Transpilers.EmitDelegate(TryWitnessOpenLock))
            .InstructionEnumeration();
    }

    private static bool TryWitnessOpenLock(Point pos, Chara cc, Chara? target, int radius, Func<Chara, bool>? func)
    {
        if (KocMod.SkipNext()) {
            return false;
        }

        var difficulty = 0f;
        var detection = Config.DetectionRadius!.Value;
        var mod = Config.DifficultyModifier!.Value;
        var skill = (cc.Evalue(SKILL.lockpicking) + cc.DEX) / 2f;

        var witnesses = pos.ListWitnesses(cc, detection).Count;
        var caught = pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
            var los = w.CanSee(cc) ? 50 : 0;
            var perception = w.PER * (75 + los) / 100;

            var randomCost = EClass.rndf(perception + mod);
            difficulty += randomCost;

            return randomCost > skill;
        });

        var suspicion = difficulty / (cc.DEX * witnesses + 1f);
        KocMod.DoModKarma(caught, cc, -1, suspicion >= 0.65f, witnesses);
        return caught;
    }
}
