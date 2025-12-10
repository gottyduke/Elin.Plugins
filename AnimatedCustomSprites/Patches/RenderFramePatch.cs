using ACS.Components;
using HarmonyLib;
using UnityEngine;

namespace ACS.Patches;

[HarmonyPatch]
internal class RenderFramePatch
{
    private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardActor), nameof(CardActor.OnRender))]
    internal static void OnRenderMpb(CardActor __instance, int ___spriteIndex)
    {
        var data = __instance.owner.sourceCard.replacer.data;
        if (data?.sprites?.Length is not > 1) {
            return;
        }

        var acs = __instance.GetOrCreate<AcsController>();
        var frame = acs.IsPlaying ? acs.CurrentFrame() : data.sprites.TryGet(___spriteIndex, true);
        if (frame == null) {
            return;
        }

        __instance.sr.sprite = frame;
        __instance.mpb.SetTexture(_mainTex, frame.texture);
    }
}