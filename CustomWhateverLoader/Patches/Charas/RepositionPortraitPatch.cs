using System.Linq;
using Cwl.API.Custom;
using Cwl.Helper.Unity;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class RepositionPortraitPatch
{
    // calculated from all game npc average
    private const float AverageDistance = 54.5f;

    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Portrait), nameof(Portrait.SetChara))]
    internal static void OnSetCharaPortrait(Portrait __instance, Chara? c)
    {
        if (c?.IsPC is not false || c.source?.id is null || ELayer.ui?.TopLayer is not LayerQuestBoard) {
            return;
        }

        if (__instance.imageChara?.sprite == null) {
            return;
        }

        if (!CwlConfig.FixBaseGameAvatar && CustomChara.All.All(adv => adv != c.source.id)) {
            return;
        }

        var sprite = __instance.imageChara.sprite;
        // do a 4 x 4 raycasts from top to bottom to determine the distance to adjust the portrait
        // I hate raycasts
        var dist = sprite.NearestPerceivableMulticast(4, 4,
            (int)sprite.rect.width / 2,
            (int)sprite.rect.height,
            directionY: -1);

        // in case some mods used non-standard sizes
        var scaler = sprite.rect.height / 128f;
        var scaledAverageDistance = AverageDistance * scaler;
        var scaledDist = dist / scaler;
        if (scaledDist is > AverageDistance or <= 0f) {
            return;
        }

        var pos = __instance.imageChara.transform.localPosition;
        var yOffset = (scaledAverageDistance - scaledDist) / 2;
        __instance.imageChara.transform.localPosition = pos with { y = pos.y / scaler - yOffset };
    }
}