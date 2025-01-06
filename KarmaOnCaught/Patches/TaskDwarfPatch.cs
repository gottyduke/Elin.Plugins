using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class TaskDwarfPatch
{
    private static bool _applied;

    private static KocConfig.Patch Config => KocConfig.Managed["Dwarf"];

    internal static bool Prepare()
    {
        if (!Config.Enabled!.Value) {
            return Config.Enabled!.Value;
        }

        OnModKarmaPatch.ToRemove.Add(
            AccessTools.Method(typeof(TaskDig), nameof(TaskDig.OnProgressComplete)));
        OnModKarmaPatch.ToRemove.Add(
            AccessTools.Method(typeof(TaskMine), nameof(TaskMine.OnProgressComplete)));

        return true;
    }

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(TaskDig), nameof(TaskDig.OnCreateProgress)),
            AccessTools.Method(typeof(TaskMine), nameof(TaskDig.OnCreateProgress)),
        ];
    }

    [HarmonyTranspiler]
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
            harmony.Patch(onProgress, transpiler: new(typeof(TaskDwarfPatch), nameof(OnCrimeWitnessIl)));
            KocMod.Log("patched TaskDwarf.OnCreateProgress/onProgressFunctor");
        } else {
            KocMod.Log("failed to apply TaskDwarf.OnCreateProgress/onProgressFunctor");
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
                Transpilers.EmitDelegate(TryWitnessDwarf))
            .InstructionEnumeration();
    }

    private static bool TryWitnessDwarf(Point pos, Chara cc, Chara? target, int radius, Func<Chara, bool>? func)
    {
        if (KocMod.SkipNext()) {
            return false;
        }

        var difficulty = 0f;
        var detection = Config.DetectionRadius!.Value;
        var mod = Config.DifficultyModifier!.Value;
        var skill = (cc.Evalue("mining") + cc.DEX) / 2f;

        var witnesses = pos.ListWitnesses(cc, detection).Count;
        var caught = pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
            var los = w.CanSee(cc) ? 50 : 0;
            var perception = w.PER * (150 + los) / 100;

            var randomCost = EClass.rndf(perception + mod);
            difficulty += randomCost;

            return randomCost > skill;
        });

        var suspicion = difficulty / (cc.DEX * witnesses + 1f);
        KocMod.DoModKarma(caught, cc, -1, suspicion >= 0.65f, witnesses);
        return caught;
    }
}