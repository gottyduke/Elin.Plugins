﻿using System.Collections.Generic;
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
            .MatchEndBackwards(
                new OperandContains(OpCodes.Ldsfld, nameof(LayerDrama.keepBGM)),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ceq),
                new OperandContains(OpCodes.Callvirt, nameof(SoundManager.SwitchPlaylist)))
            .ThrowIfInvalid("failed to match")
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0))
            .SetInstruction(
                Transpilers.EmitDelegate(TryMergeNewPlaylist))
            .InstructionEnumeration();
    }

    private static void TryMergeNewPlaylist(SoundManager sm, Playlist mold, bool stopBGM, Zone zone)
    {
        var merged = CustomPlaylist.GeneratePlaylistForZone(mold, zone);
        if (merged == null || merged.list.Count == 0) {
            sm.SwitchPlaylist(mold);
            return;
        }

        if (!TryStreaming(merged)) {
            sm.SwitchPlaylist(merged);
        }
    }

    [SwallowExceptions]
    private static bool TryStreaming(Playlist newList)
    {
        if (!CwlConfig.SeamlessStreaming) {
            return false;
        }

        var sm = SoundManager.current;
        var currentStreaming = newList.list.FindIndex(bgm => bgm.data == sm.currentBGM.data);
        if (currentStreaming == -1) {
            return false;
        }

        sm.SetBGMPlaylist(newList);
        sm.currentPlaylist = newList;
        newList.currentItem = newList.list[currentStreaming];

        if (newList.nextIndex == currentStreaming) {
            newList.nextIndex = (currentStreaming + 1) % newList.list.Count;
        }

        return true;
    }
}