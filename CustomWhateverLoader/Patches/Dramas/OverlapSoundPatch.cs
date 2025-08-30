using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch]
internal class OverlapSoundPatch
{
    private static bool _patched;
    private static SoundSource? _lastPlayed;

    internal static bool Prepare()
    {
        return CwlConfig.NoOverlappingSounds;
    }

    // patch so that sounds don't overlap in dialogs
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
    internal static IEnumerable<CodeInstruction> OnSoundPlayIl(IEnumerable<CodeInstruction> instructions)
    {
        if (_patched) {
            return instructions;
        }

        _patched = true;

        var cm = new CodeMatcher(instructions);
        CodeMatch[] soundSwitch = [
            new(OpCodes.Ldloc_S),
            new(OpCodes.Ldstr, "sound"),
            new(OpCodes.Call),
            new(OpCodes.Brtrue),
        ];

        if (!cm.MatchEndForward(soundSwitch).IsValid || cm.Operand is not Label label) {
            return cm.InstructionEnumeration();
        }

        CodeMatch[] soundPlayFunctor = [
            new OperandMatch(OpCodes.Ldarg_0, o => o.labels.Contains(label)),
            new(OpCodes.Ldloc_S),
            new(OpCodes.Ldfld),
            new(OpCodes.Ldftn),
        ];

        if (!cm.MatchEndForward(soundPlayFunctor).IsValid || cm.Operand is not MethodInfo mi) {
            return cm.InstructionEnumeration();
        }

        var harmony = new Harmony(ModInfo.Guid);
        harmony.Patch(mi, transpiler: new(typeof(OverlapSoundPatch), nameof(InternalSoundStopperIl)));

        return cm.InstructionEnumeration();
    }

    internal static IEnumerable<CodeInstruction> InternalSoundStopperIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchStartBackwards(
                new OperandContains(OpCodes.Callvirt, nameof(SoundManager.Play)),
                new(OpCodes.Pop))
            .EnsureValid("sound play stopper")
            .SetInstruction(
                Transpilers.EmitDelegate(NoOverlappingPlay))
            .InstructionEnumeration();
    }

    [SwallowExceptions]
    private static SoundSource NoOverlappingPlay(SoundManager sm, string id)
    {
        _lastPlayed?.Stop();
        _lastPlayed = sm.Play(id);
        return _lastPlayed;
    }
}