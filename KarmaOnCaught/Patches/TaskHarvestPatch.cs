using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class TaskHarvestPatch
{
    private static bool _applied;

    private static KocConfig.Patch Config => KocConfig.Managed["Harvest"];

    internal static bool Prepare()
    {
        return Config.Enabled!.Value;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaskHarvest), nameof(TaskHarvest.OnCreateProgress))]
    internal static IEnumerable<CodeInstruction> OnCreateProgressIl(IEnumerable<CodeInstruction> instructions)
    {
        if (_applied) {
            return instructions;
        }

        _applied = true;

        var cm = new CodeMatcher(instructions);
        var harmony = new Harmony(ModInfo.Guid);

        CodeMatch[] onProgressFunctor = [
            new(OpCodes.Ldftn),
            new(OpCodes.Newobj),
            new(OpCodes.Stfld, AccessTools.Field(
                typeof(Progress_Custom),
                nameof(Progress_Custom.onProgress))),
        ];

        if (cm.MatchStartForward(onProgressFunctor).IsValid && cm.Operand is MethodInfo onProgress) {
            harmony.Patch(onProgress, transpiler: new(typeof(TaskHarvestPatch), nameof(OnCrimeWitnessIl)));
            KocMod.Log("patched TaskHarvest.OnCreateProgress/onProgressFunctor");
        } else {
            KocMod.Log("failed to apply TaskHarvest.OnCreateProgress/onProgressFunctor");
        }

        CodeMatch[] onProgressCompleteFunctor = [
            new(OpCodes.Ldftn),
            new(OpCodes.Newobj),
            new(OpCodes.Stfld, AccessTools.Field(
                typeof(Progress_Custom),
                nameof(Progress_Custom.onProgressComplete))),
        ];

        if (cm.MatchStartForward(onProgressCompleteFunctor).IsValid && cm.Operand is MethodInfo onProgressComplete) {
            OnModKarmaPatch.ToRemove.Add(onProgressComplete);
        } else {
            KocMod.Log("failed to apply TaskHarvest.OnProgressComplete/onProgressCompleteFunctor");
        }

        return cm.InstructionEnumeration();
    }

    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchEndBackwards(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(Point),
                    nameof(Point.TryWitnessCrime))))
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