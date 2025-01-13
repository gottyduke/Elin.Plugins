using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class AIStealPatch
{
    private static bool _applied;

    private static KocConfig.Patch Config => KocConfig.Managed["Steal"];

    internal static bool Prepare()
    {
        return Config.Enabled?.Value ?? false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AI_Steal), nameof(AI_Steal.Run), MethodType.Enumerator)]
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
            harmony.Patch(onProgress, transpiler: new(typeof(AIStealPatch), nameof(OnCrimeWitnessIl)));
            KocMod.Log("patched AI_Steal.Run/onProgressFunctor");
        } else {
            KocMod.Log("failed to apply AI_Steal.Run/onProgressFunctor");
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

        if (!cc.currentZone.AllowCriminal) {
            pos.CallGuard(cc, witness);
            KocMod.DoModKarma(true, cc, -1, false, witnesses.Count);
        }

        target?.DoHostileAction(cc);
        if (witness != target) {
            witness.DoHostileAction(cc);
        }

        return false;
    }
}