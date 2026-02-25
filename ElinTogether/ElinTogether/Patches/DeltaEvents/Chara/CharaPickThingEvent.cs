using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Pick))]
internal static class CharaPickThingEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaPickThingy(Chara __instance, Thing t)
    {
        if (CharaProgressCompleteEvent.IsHappening) {
            if (NetSession.Instance.IsHost && !CharaProgressCompleteEvent.Actions.TryGetValue(t, out _)) {
                CharaProgressCompleteEvent.Actions[t] = new CharaPickThingDelta {
                    Owner = CharaProgressCompleteEvent.Chara!,
                    Thing = t,
                    Pos = null,
                    Type = CharaPickThingDelta.PickType.Pick,
                };
            }

            return false;
        }

        var session = NetSession.Instance;
        if (session.Connection is not { } connection) {
            return true;
        }

        // we are host, propagate to everyone
        // we are client, only propagate ourselves
        if (connection.IsHost || __instance.IsPC) {
            connection.Delta.AddRemote(new CharaPickThingDelta {
                Owner = __instance,
                Thing = t,
                Pos = null,
                Type = CharaPickThingDelta.PickType.Pick,
            });
        }

        return true;
    }
}

[HarmonyPatch(typeof(Chara), nameof(Chara.PickOrDrop), [typeof(Point), typeof(Thing), typeof(bool)])]
internal static class CharaPickOrDropEvent
{
    [HarmonyPrefix]
    internal static bool OnCharaPickOrDrop(Chara __instance, Point p, Thing t)
    {
        if (!CharaProgressCompleteEvent.IsHappening) {
            return true;
        }

        if (NetSession.Instance.IsHost && !CharaProgressCompleteEvent.Actions.TryGetValue(t, out _)) {
            CharaProgressCompleteEvent.Actions[t] = new CharaPickThingDelta {
                Owner = CharaProgressCompleteEvent.Chara!,
                Thing = t,
                Pos = p,
                Type = CharaPickThingDelta.PickType.PickOrDrop,
            };

            CardCache.KeepAlive(t);
        }

        return false;
    }
}

[HarmonyPatch(typeof(Map), nameof(Map.TrySmoothPick), [typeof(Point), typeof(Thing), typeof(Chara)])]
internal static class CharaTrySmoothPickEvent
{
    [HarmonyPrefix]
    internal static bool OnTrySmoothPick(Point p, Thing t, Chara c)
    {
        if (!CharaProgressCompleteEvent.IsHappening) {
            return true;
        }

        if (NetSession.Instance.IsHost && !CharaProgressCompleteEvent.Actions.TryGetValue(t, out _)) {
            CharaProgressCompleteEvent.Actions[t] = new CharaPickThingDelta {
                Owner = CharaProgressCompleteEvent.Chara!,
                Thing = t,
                Pos = p,
                Type = CharaPickThingDelta.PickType.TrySmoothPick,
            };

            CardCache.KeepAlive(t);
        }

        return false;
    }
}