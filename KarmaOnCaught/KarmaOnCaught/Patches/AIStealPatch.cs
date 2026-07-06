using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EModding.Helper;
using HarmonyLib;

namespace KarmaOnCaught.Patches;

[HarmonyPatch]
internal class AIStealPatch
{
    private static KocConfig.Patch Config => KocConfig.Managed["Steal"];

    internal static bool Prepare()
    {
        if (!Config.Enabled!.Value) {
            return false;
        }

        OnModKarmaPatch.ToRemove.Add(AccessTools.Method("AI_Steal+<>c__DisplayClass9_0:<Run>b__3", []));

        return true;
    }

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method("AI_Steal+<>c__DisplayClass9_0:<Run>b__2", [typeof(Progress_Custom)]);
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchEndBackwards(
                new OperandContains(OpCodes.Callvirt, nameof(Point.TryWitnessCrime)))
            .EnsureValid("pos.TryWitnessPickpocket")
            .SetAndAdvance(OpCodes.Call, AccessTools.Method(
                typeof(AIStealPatch),
                nameof(TryWitnessPickpocket)))
            .InstructionEnumeration();
    }

    private static bool TryWitnessPickpocket(Point pos, Chara cc, Chara? target, int _, Func<Chara, bool> func)
    {
        if (KocMod.SkipNext()) {
            return false;
        }

        var detection = Config.DetectionRadius!.Value;
        var witnesses = pos.ListWitnesses(cc, detection, target: target);
        var witness = witnesses.Find(witness => !witness.IsHostile() && func(witness));
        if (witness is null) {
            return false;
        }

        if (cc.currentZone.AllowCriminal) {
            Msg.SetColor("bad");
            Msg.Say(KocLoc.CaughtPrompt);
            if (witnesses.Count != 0) {
                Msg.Say(KocLoc.WithWitness(witnesses.Count));
            }
        } else {
            pos.CallGuard(cc, witness);
            KocMod.DoModKarma(true, cc, -1, false, witnesses.Count);
        }

        target?.DoHostileAction(cc);
        if (witness != target) {
            witness.DoHostileAction(cc);
        }

        return true;
    }
}