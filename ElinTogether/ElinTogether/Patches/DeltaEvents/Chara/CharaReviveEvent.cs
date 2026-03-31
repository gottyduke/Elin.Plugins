using System;
using ElinTogether.Elements;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaReviveEvent
{
    private static string? LastWords;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.MakeGrave))]
    internal static bool OnCharaMakeGrave(Chara __instance, string lastword)
    {
        if (NetSession.Instance.Connection is not ElinNetClient || !__instance.IsPC) {
            return true;
        }

        LastWords = lastword;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.Revive))]
    internal static bool OnCharaRevive(Chara __instance)
    {
        if (NetSession.Instance.Connection is not ElinNetClient client || ElinDelta.IsApplying) {
            return true;
        }

        // drop all other character revives and wait for delta
        if (!__instance.IsPC) {
            return false;
        }

        client.Delta.AddRemote(new CharaReviveDelta {
            Owner = __instance,
            LastWords = LastWords,
        });

        return true;
    }
}