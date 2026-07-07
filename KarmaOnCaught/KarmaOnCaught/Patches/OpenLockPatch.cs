using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EModding.Helper;
using HarmonyLib;

namespace KarmaOnCaught.Patches;

[HarmonyPatch]
internal class OpenLockPatch
{
    private static KocConfig.Patch Config => KocConfig.Managed["Lockpick"];

    internal static bool Prepare()
    {
        if (!Config.Enabled!.Value) {
            return false;
        }

        OnModKarmaPatch.ToRemove.Add(
            AccessTools.Method(typeof(Trait), nameof(Trait.OnLockOpen)));

        return true;
    }

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method("AI_OpenLock:<CreateProgress>b__8_1", [typeof(Progress_Custom)]);
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchEndBackwards(
                new OperandContains(OpCodes.Callvirt, nameof(Point.TryWitnessCrime)))
            .EnsureValid("pos.TryWitnessLockPick")
            .RemoveInstruction()
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryWitnessLockPick))
            .InstructionEnumeration();
    }

    private static bool TryWitnessLockPick(Point pos,
                                           Chara cc,
                                           Chara? target,
                                           int radius,
                                           Func<Chara, bool>? func,
                                           AI_OpenLock act)
    {
        if (KocMod.SkipNext()) {
            return false;
        }

        var difficulty = 0f;
        var detection = Config.DetectionRadius!.Value;
        var mod = Config.DifficultyModifier!.Value;
        var skill = (cc.Evalue(SKILL.lockpicking) + cc.DEX) / 2f;

        var chest = act.target;
        var lockLv = chest.c_lockLv;
        var witnesses = chest.pos.ListWitnesses(cc, detection).Count;
        var caught = chest.pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
            var los = w.CanSee(cc) ? 50 : 0;
            var perception = w.PER * (75 + los) / 100;

            var randomCost = EClass.rndf(perception + lockLv + mod);
            difficulty += randomCost;

            return randomCost > skill;
        });

        var suspicion = difficulty / skill;
        KocMod.DoModKarma(caught, cc, -8, suspicion >= 0.65f, witnesses);
        return caught;
    }
}