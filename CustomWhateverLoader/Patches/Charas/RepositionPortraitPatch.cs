using System.Collections.Generic;
using System.Linq;
using Cwl.API.Custom;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine.UI;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class RepositionPortraitPatch
{
    // calculated from all game npc average
    private const float AverageDistance = 54.5f;

    private static readonly Dictionary<int, float> _cached = [];

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

        if (!CwlConfig.FixBaseGameAvatar && CustomChara.All.All(chara => chara != c.source.id)) {
            return;
        }

        Reposition(__instance.imageChara, c.uid);
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemGeneral), nameof(ItemGeneral.SetChara))]
    internal static void OnSetCharaButton(ItemGeneral __instance, Chara c)
    {
        if (CustomChara.All.All(chara => chara != c.source.id)) {
            return;
        }

        Reposition(__instance.button1.icon, c.uid);
    }

    private static void Reposition(Image image, int uid)
    {
        var sprite = image.sprite;
        var scaler = sprite.rect.height / 128f;

        if (!_cached.TryGetValue(uid, out var yOffset)) {
            // do a 4 x 4 raycasts from top to bottom to determine the distance to adjust the portrait
            // I hate raycasts
            var dist = sprite.NearestPerceivableMulticast(4, 4,
                (int)sprite.rect.width / 2,
                (int)sprite.rect.height,
                directionY: -1);

            // in case some mods used non-standard sizes
            var scaledAverageDistance = AverageDistance * scaler;
            var scaledDist = dist / scaler;
            if (scaledDist is > AverageDistance or <= 0f) {
                return;
            }

            _cached[uid] = yOffset = (scaledAverageDistance - scaledDist) / 2;
        }

        var pos = image.transform.localPosition;
        image.transform.localPosition = pos with { y = pos.y / scaler - yOffset };
    }
}