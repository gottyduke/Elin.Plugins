using System.Reflection;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class InvRerollEvent
{
    private static InvRerollDelta? DeferredDelta;

    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(
            AccessTools.FirstInner(typeof(UIInventory), t => t.Name.Contains("DisplayClass71_10")), "<RefreshMenu>b__48");
    }

    [HarmonyPrefix]
    internal static bool OnInvReroll(object __instance)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        var field = AccessTools.Field(__instance.GetType(), "_owner");
        var owner = field.GetValue(__instance) as Card;
        var cost = owner!.trait.CostRerollShop;
        if (EMono._zone.influence < cost) {
            return true;
        }

        DeferredDelta = new InvRerollDelta {
            ShopOwner = owner,
        };

        return connection.IsHost;
    }

    [HarmonyPostfix]
    internal static void OnInvRerollEnd()
    {
        if (DeferredDelta is null) {
            return;
        }

        NetSession.Instance.Connection?.Delta.AddRemote(DeferredDelta);
        DeferredDelta = null;
    }
}