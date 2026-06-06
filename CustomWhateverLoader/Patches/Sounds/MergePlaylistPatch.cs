using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class MergePlaylistPatch
{
    private static Lot? _lotDeferred;

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
            .EnsureValid("switch playlist")
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0))
            .SetInstruction(
                Transpilers.EmitDelegate(TryMergeNewPlaylist))
            .InstructionEnumeration();
    }

    private static void TryMergeNewPlaylist(SoundManager sm, Playlist mold, bool stopBGM, Zone zone)
    {
        try {
            var merged = CustomPlaylist.GeneratePlaylistForZone(mold, zone);
            if (merged == null || merged.list.Count == 0) {
                sm.SwitchPlaylist(mold, stopBGM);
                return;
            }

            if (mold == SoundManager.current.plLot) {
                if (_lotDeferred is null) {
                    _lotDeferred = EClass.pc?.pos?.cell?.room?.lot;
                    CoroutineHelper.Deferred(DeferredRefresh, ShouldRefresh);
                }
            }

            if (TryPersistBossBgm(zone.Boss)) {
            }

            if (!TryStreaming(merged)) {
                sm.SwitchPlaylist(merged, stopBGM);
            }
        } catch (Exception ex) {
            sm.SwitchPlaylist(mold, stopBGM);
            CwlMod.WarnWithPopup<CustomPlaylist>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }
    }

    [SwallowExceptions]
    private static bool TryStreaming(Playlist newList)
    {
        if (!CwlConfig.SeamlessStreaming) {
            return false;
        }

        var sm = SoundManager.current;
        if (sm.currentBGM?.data == null) {
            return false;
        }

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

    private static bool TryPersistBossBgm(Chara boss)
    {
        return true;
    }

    private static void DeferredRefresh()
    {
        EClass.core.game?.activeZone?.RefreshBGM();
        _lotDeferred = null;
    }

    private static bool ShouldRefresh()
    {
        var lot = EClass.core.game?.player?.chara?.pos?.cell?.room?.lot;
        return !EClass.core.IsGameStarted || lot is null || lot != _lotDeferred;
    }
}