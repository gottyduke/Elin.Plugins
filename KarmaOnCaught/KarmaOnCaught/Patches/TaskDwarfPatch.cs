using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EModding.Helper;
using HarmonyLib;

namespace KarmaOnCaught.Patches;

[HarmonyPatch]
internal class TaskDwarfPatch
{
    private static KocConfig.Patch Config => KocConfig.Managed["Dwarf"];

    internal static bool Prepare()
    {
        if (!Config.Enabled!.Value) {
            return false;
        }

        OnModKarmaPatch.ToRemove.Add(
            AccessTools.Method(typeof(TaskDig), nameof(TaskDig.OnProgressComplete)));
        OnModKarmaPatch.ToRemove.Add(
            AccessTools.Method(typeof(TaskMine), nameof(TaskMine.OnProgressComplete)));

        OnModKarmaPatch.ToRemove.Add(AccessTools.Method(typeof(TaskDig), "<OnProgressComplete>g__Dig|22_0"));

        return true;
    }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            AccessTools.Method("TaskDig+<>c__DisplayClass19_0:<OnCreateProgress>b__1", [typeof(Progress_Custom)]),
            AccessTools.Method("TaskMine+<>c__DisplayClass23_0:<OnCreateProgress>b__1", [typeof(Progress_Custom)]),
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchEndBackwards(
                new OperandContains(OpCodes.Callvirt, nameof(Point.TryWitnessCrime)))
            .EnsureValid("pos.TryWitnessDwarf")
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
        var skill = (cc.Evalue(SKILL.mining) + cc.DEX) / 2f;

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
