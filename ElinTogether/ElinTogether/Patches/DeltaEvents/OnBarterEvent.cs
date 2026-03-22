using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
internal class OnBarterEvent
{
    private static OnBarterDelta? DeferredDelta;
    [HarmonyPrefix]
    internal static bool OnBarter(Trait __instance)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        var owner = __instance.owner;
        if (connection.IsClient && owner.things.Find("chest_merchant") is null) {
            // create a temp chest
            var chest = ThingGen.Create("chest_merchant");
            chest.parent = owner;
            owner.things.Add(chest);
        }

        DeferredDelta = new OnBarterDelta {
            ShopOwner = owner,
        };

        return connection.IsHost;
    }

    [HarmonyPostfix]
    internal static void OnBarterEnd()
    {
        if (DeferredDelta is null) {
            return;
        }

        NetSession.Instance.Connection?.Delta.AddRemote(DeferredDelta);
        DeferredDelta = null;
    }
}