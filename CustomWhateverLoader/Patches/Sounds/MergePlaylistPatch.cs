using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class MergePlaylistPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Zone), nameof(Zone.RefreshBGM))]
    internal static IEnumerable<CodeInstruction> OnSetNewPlaylistIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .End()
            .MatchStartBackwards(
                new OperandContains(OpCodes.Call, nameof(EClass.Sound)),
                new(OpCodes.Ldloc_0),
                new OperandContains(OpCodes.Ldsfld, nameof(LayerDrama.keepBGM)))
            .ThrowIfInvalid("failed to match")
            .Advance(2)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(CustomPlaylist.GeneratePlaylistForZone))
            .InstructionEnumeration();
    }
}