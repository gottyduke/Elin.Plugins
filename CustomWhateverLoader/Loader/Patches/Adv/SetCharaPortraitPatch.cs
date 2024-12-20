﻿using System.Collections.Generic;
using System.Linq;
using Cwl.API;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Loader.Patches.Adv;

[HarmonyPatch]
internal class SetCharaPortraitPatch
{
    // calculated from all game npc average
    private const float AverageDistance = 54.5f;
    private static readonly Dictionary<string, Vector3> _cached = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Portrait), nameof(Portrait.SetChara))]
    internal static void OnSetCharaPortrait(Portrait __instance, Chara c)
    {
        if (c.IsPC) {
            return;
        }

        if (!CwlConfig.FixBaseGameAvatar &&
            CustomAdventurer.All.All(adv => adv != c.source.id)) {
            return;
        }

        if (!_cached.TryGetValue(c.source.id, out var cached)) {
            var sprite = __instance.imageChara.sprite;

            var dist = sprite.NearestPerceivableMulticast(4, 4,
                (int)sprite.rect.width / 2,
                (int)sprite.rect.height,
                directionY: -1);

            if (dist > AverageDistance) {
                return;
            }

            var pos = __instance.imageChara.transform.localPosition;
            cached = pos with { y = pos.y - (AverageDistance - dist) / 2 };
            _cached[c.source.id] = cached;
        }

        __instance.imageChara.transform.localPosition = cached;
    }
}