using Cwl.API.Custom;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Religions;

[HarmonyPatch]
internal class SetReligionPortraitPatch
{
    private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LayerDrama), nameof(LayerDrama.Activate))]
    internal static void OnSetupWorshipAct()
    {
        if (LayerDrama.currentReligion is not CustomReligion custom) {
            return;
        }

        if (LayerDrama.Instance?.setup?.step != "worship") {
            return;
        }

        var portrait = LayerDrama.Instance.drama.dialog.portrait?.portrait;
        var sprite = custom.GetSprite();

        portrait?.sprite = sprite;
        portrait?.material.SetTexture(_mainTex, sprite?.texture);
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Religion), nameof(Religion.GetSprite))]
    internal static void OnSetReligionSprite(Religion __instance, ref Sprite __result)
    {
        var overrideSprite = __instance.id.LoadSprite(resizeWidth: 150, resizeHeight: 200);
        if (overrideSprite == null) {
            return;
        }

        __result = overrideSprite;
    }
}