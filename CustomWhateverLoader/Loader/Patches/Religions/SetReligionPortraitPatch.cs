using Cwl.API.Custom;
using Cwl.Helper.Unity;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

namespace Cwl.Loader.Patches.Religions;

[HarmonyPatch]
internal class SetReligionPortraitPatch
{
    private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

    [Time]
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

        if (!SpriteReplacer.dictModItems.TryGetValue(custom.id, out var file)) {
            return;
        }

        var portrait = LayerDrama.Instance.drama.dialog.portrait.portrait;
        var sprite = $"{file}.png".LoadSprite();
        sprite ??= portrait.sprite;

        portrait.sprite = sprite;
        portrait.material?.SetTexture(_mainTex, sprite.texture);
    }
}