using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EModding.Helper;
using HarmonyLib;

namespace KarmaOnCaught.Patches;

[HarmonyPatch]
internal class TaskHarvestPatch
{
    private static KocConfig.Patch Config => KocConfig.Managed["Harvest"];

    internal static bool Prepare()
    {
        return Config.Enabled!.Value;
    }

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method("TaskHarvest+<>c__DisplayClass27_0:<OnCreateProgress>b__1", [typeof(Progress_Custom)]);
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchEndBackwards(
                new OperandContains(OpCodes.Callvirt, nameof(Point.TryWitnessCrime)))
            .EnsureValid("pos.TryWitnessHarvest")
            .SetInstruction(
                Transpilers.EmitDelegate(TryWitnessHarvest))
            .InstructionEnumeration();
    }

    private static bool TryWitnessHarvest(Point pos, Chara cc, Chara? target, int radius, Func<Chara, bool>? func)
    {
        if (KocMod.SkipNext()) {
            return false;
        }

        var difficulty = 0f;
        var detection = Config.DetectionRadius!.Value;
        var mod = Config.DifficultyModifier!.Value;

        var witnesses = pos.ListWitnesses(cc, detection).Count;
        var caught = pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
            var los = w.CanSee(cc) ? 50 : 0;
            var perception = w.PER * (150 + los) / 100;

            var randomCost = EClass.rndf(perception + mod);
            difficulty += randomCost;

            return randomCost > cc.DEX;
        });

        var suspicion = difficulty / (cc.DEX * witnesses + 1f);
        KocMod.DoModKarma(caught, cc, -1, suspicion >= 0.65f, witnesses);
        return caught;
    }
}