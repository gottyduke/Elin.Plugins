using Cwl.API;
using Cwl.Helper.Unity;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

namespace Cwl.Loader.Patches;

[HarmonyPatch]
internal class LoadSpritePatch
{
    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Religion), nameof(Religion.GetSprite))]
    internal static bool OnGetReligionSprite(ref Sprite? __result, Religion __instance)
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
    internal static bool OnGetDomainSprite(ref Sprite? __result, Domain __instance)
    {
        var id = __instance.source.alias[3..].ToLower();
        if (SpriteReplacer.dictModItems.TryGetValue(id, out var file)) {
            __result = $"{file}.png".LoadSprite();
        }

        return __result == null;
    }

    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Faction), nameof(Faction.GetSprite))]
    internal static bool OnGetFactionSprite(ref Sprite? __result, Faction __instance)
    {
        var id = __instance.source.id;
        if (SpriteReplacer.dictModItems.TryGetValue(id, out var file)) {
            __result = $"{file}.png".LoadSprite();
        }

        return __result == null;
    }
}