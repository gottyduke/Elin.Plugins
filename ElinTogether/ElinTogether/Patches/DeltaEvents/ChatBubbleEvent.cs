using System.Collections.Generic;
using System.Reflection;
using ElinTogether.Helper;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class ChatBubbleEvent
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardRenderer), nameof(CardRenderer.Say))]
    internal static bool OnCardTalkRaw(CardRenderer __instance, string text, float duration)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return true;
        }

        var chara = __instance.owner.Chara;
        if ((connection.IsHost && !chara.IsRemotePlayer) || chara.IsPC) {
            connection.Delta.AddRemote(new CardRendererTalkDelta {
                Card = chara,
                Text = text,
                Duration = duration,
            });
            return true;
        }

        return ElinDelta.IsApplying;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv.OnEnterChat))]
    internal static void OnEnterChat()
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        var text = EClass.game.log.dict[EClass.game.log.currentLogIndex - 1].text;
        var color = MsgBlock.lastBlock.txt.color;
        connection.Delta.AddRemote(new MsgSayDelta {
            Text = text,
            R = color.r,
            G = color.g,
            B = color.b,
            A = color.a,
        });
    }

    [HarmonyPatch]
    internal static class MsgSaySynchronizationContext
    {
        internal static IEnumerable<MethodInfo> TargetMethods()
        {
            return [
                AccessTools.Method(typeof(Card), nameof(Card.TalkRaw)),
                AccessTools.Method(typeof(Chara), nameof(Chara.TalkTopic)),
            ];
        }

        internal static bool Prefix(out int __state)
        {
            __state = EClass.game.log.currentLogIndex;
            return NetSession.Instance.IsHost;
        }

        internal static void Postfix(int __state)
        {
            if (NetSession.Instance.Connection is not ElinNetHost host) {
                return;
            }

            if (__state == EClass.game.log.currentLogIndex) {
                return;
            }

            var text = EClass.game.log.dict[EClass.game.log.currentLogIndex - 1].text;
            var color = MsgBlock.lastBlock.txt.color;
            host.Delta.AddRemote(new MsgSayDelta {
                Text = text,
                R = color.r,
                G = color.g,
                B = color.b,
                A = color.a,
            });
        }
    }
}