using Cwl.API.Drama;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class DramaPortraitOverride
{
    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Portrait), nameof(Portrait.SetPerson))]
    internal static void OnSetDialogActorPortrait(Portrait __instance, Person p)
    {
        if (EMono.ui.TopLayer is not LayerDrama layer || layer.drama != DramaExpansion.Cookie?.Dm) {
            return;
        }

        if (!p.hasChara || p.idPortrait == p.chara.GetIdPortrait()) {
            return;
        }

        var tint = p.chara.pccData?.GetHairColor(true) ?? Color.white;
        __instance.SetPortrait(p.idPortrait, tint);
    }
}