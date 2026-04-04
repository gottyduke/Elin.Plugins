using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaReviveEvent
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.MakeGrave))]
    internal static bool OnCharaMakeGrave(Chara __instance, string lastword, out string? __state)
    {
        __state = null;

        if (NetSession.Instance.Connection is not ElinNetClient || !__instance.IsPC) {
            return true;
        }

        __state = lastword;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.Revive))]
    internal static bool OnCharaRevive(Chara __instance, string? __state)
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
            LastWords = __state,
        });

        return true;
    }
}