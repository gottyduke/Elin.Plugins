using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class RemoteScenePausePatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Scene), nameof(Scene.OnUpdate))]
    internal static IEnumerable<CodeInstruction> OnSetPauseIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Stfld, nameof(Scene.paused)))
            .EnsureValid("set Scene.paused")
            .Advance(1)
            .InsertAndAdvance(
                Transpilers.EmitDelegate(SetSharedPausedState))
            .InstructionEnumeration();
    }

    private static void SetSharedPausedState()
    {
        var scene = EMono.scene;
        if (!scene.paused) {
            return;
        }

        if (NetSession.Instance.Connection is not ElinNetHost  host) {
            return;
        }

        if (host.SharedActState > 0) {
            scene.paused = false;
        }
    }
}