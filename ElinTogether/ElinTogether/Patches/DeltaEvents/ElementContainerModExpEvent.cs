using ElinTogether.Helper;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(ElementContainer), nameof(ElementContainer.ModExp))]
internal class ElementContainerModExpEvent
{
    [HarmonyPrefix]
    internal static bool OnModExp(ElementContainer __instance, out bool __state)
    {
        if (NetSession.Instance.Connection is not { } connection
            || __instance.Card is not Chara chara
            || ElinDelta.IsApplying) {
            __state = false;
            return true;
        }

        if (connection.IsHost && chara.IsRemotePlayer is true) {
            __state = false;
            return false;
        }

        if (connection.IsClient && chara.IsPC is not true) {
            __state = false;
            return false;
        }

        __state = true;
        return true;
    }

    [HarmonyPostfix]
    internal static void OnModExpEnd(ElementContainer __instance, int ele, bool __state)
    {
        if (!__state) {
            return;
        }

        var element = __instance.GetElement(ele);
        NetSession.Instance.Connection!.Delta.AddRemote(new CardModExpDelta {
            Chara = __instance.Chara,
            Ele = ele,
            Base = element.vBase,
            Exp = element.vExp,
        });
    }
}