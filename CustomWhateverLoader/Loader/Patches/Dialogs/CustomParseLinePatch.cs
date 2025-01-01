using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SwallowExceptions.Fody;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class CustomParseLinePatch
{
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
        var cm = new CodeMatcher(instructions);
        CodeMatch[] soundSwitch = [
            new(OpCodes.Ldloc_S),
            new(OpCodes.Ldstr, "sound"),
            new(OpCodes.Call),
            new(OpCodes.Brtrue),
        ];

        var soundPlay = cm.MatchEndForward(soundSwitch);
        if (!soundPlay.IsValid || soundPlay.Operand is not Label label) {
            return cm.InstructionEnumeration();
        }

        CodeMatch[] soundPlayFunctor = [
            new(o => o.opcode == OpCodes.Ldarg_0 &&
                     o.labels.Contains(label)),
            new(OpCodes.Ldloc_S),
            new(OpCodes.Ldfld),
            new(OpCodes.Ldftn),
        ];

        var soundFunctor = cm.MatchEndForward(soundPlayFunctor);
        if (!soundFunctor.IsValid || soundFunctor.Operand is not MethodInfo mi) {
            return cm.InstructionEnumeration();
        }

        var harmony = new Harmony(ModInfo.Guid);
        harmony.Patch(mi, transpiler: new(typeof(CustomParseLinePatch), nameof(InternalSoundStopperIl)));

        return cm.InstructionEnumeration();
    }

    internal static IEnumerable<CodeInstruction> InternalSoundStopperIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchStartBackwards(
                new CodeMatch(o => o.opcode == OpCodes.Callvirt &&
                                   o.operand.ToString().Contains(nameof(SoundManager.Play))),
                new CodeMatch(OpCodes.Pop))
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