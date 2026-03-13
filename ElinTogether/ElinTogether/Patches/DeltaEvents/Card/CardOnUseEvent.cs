using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

internal static class CardOnUseEvent
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Trait), nameof(Trait.OnUse));
    }

    [HarmonyPrefix]
    internal static bool OnUseCard(Trait __instance, Chara chara)
    {
        if (NetSession.Instance.Connection is not ElinNetClient connection || ElinDelta.IsApplying) {
            return true;
        }

        var card = __instance.owner;
        if (!CardCache.Contains(card)) {
            return true;
        }

        connection.Delta.AddRemote(new CardOnUseDelta {
            Card = card,
            RootCard = card.GetRootCard(),
            User = chara,
        });

        return false;
    }
}