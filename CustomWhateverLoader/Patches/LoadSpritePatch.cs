using Cwl.API.Custom;
using Cwl.Helper.Unity;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

namespace Cwl.Patches;

[HarmonyPatch]
internal class LoadSpritePatch
{
    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Religion), nameof(Religion.GetSprite))]
    internal static bool OnGetReligionSprite(Religion __instance, ref Sprite? __result)
    {
        if (__instance is not CustomReligion custom) {
            return true;
        }

        if (SpriteReplacer.dictModItems.TryGetValue(custom.id, out var file)) {
            __result = $"{file}.png".LoadSprite();
        }

        return __result == null;
    }

    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Domain), nameof(Domain.GetSprite))]
    internal static bool OnGetDomainSprite(Domain __instance, ref Sprite? __result)
    {
        var id = __instance.source.alias[3..].ToLowerInvariant();
        if (SpriteReplacer.dictModItems.TryGetValue(id, out var file)) {
            __result = $"{file}.png".LoadSprite();
        }

        return __result == null;
    }

    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Faction), nameof(Faction.GetSprite))]
    internal static bool OnGetFactionSprite(Faction __instance, ref Sprite? __result)
    {
        var id = __instance.source.id;
        if (SpriteReplacer.dictModItems.TryGetValue(id, out var file)) {
            __result = $"{file}.png".LoadSprite();
        }

        return __result == null;
    }
}