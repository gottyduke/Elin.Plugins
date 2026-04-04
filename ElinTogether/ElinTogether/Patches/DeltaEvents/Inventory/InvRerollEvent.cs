using System.Reflection;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class InvRerollEvent
{
    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(
            AccessTools.FirstInner(typeof(UIInventory), t => t.Name.Contains("DisplayClass71_10")), "<RefreshMenu>b__48");
    }

    [HarmonyPrefix]
    internal static bool OnInvReroll(object __instance, out InvRerollDelta? __state)
    {
        __state = null;

        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        var field = AccessTools.Field(__instance.GetType(), "_owner");
        var owner = field.GetValue(__instance) as Card;
        var cost = owner!.trait.CostRerollShop;
        if (EMono._zone.influence < cost) {
            return true;
        }

        __state = new() {
            ShopOwner = owner,
        };

        return connection.IsHost;
    }

    [HarmonyPostfix]
    internal static void OnInvRerollEnd(InvRerollDelta? __state)
    {
        if (__state is not null) {
            NetSession.Instance.Connection?.Delta.AddRemote(__state);
        }
    }
}