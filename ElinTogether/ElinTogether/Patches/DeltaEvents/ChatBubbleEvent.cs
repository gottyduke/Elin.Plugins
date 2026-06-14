using System.Collections.Generic;
using System.Reflection;
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

        if (connection.IsHost) {
            connection.Delta.AddRemote(new CardRendererTalkDelta {
                Card = __instance.owner,
                Text = text,
                Duration = duration,
            });
            return true;
        }

        return ElinDelta.IsApplying;
    }

    [HarmonyPatch]
    internal static class MsgSaySynchronizationContext
    {
        private static int MsgIndex;

        internal static IEnumerable<MethodInfo> TargetMethods()
        {
            return [
                AccessTools.Method(typeof(Card), nameof(Card.TalkRaw)),
                AccessTools.Method(typeof(Chara), nameof(Chara.TalkTopic)),
            ];
        }

        internal static bool Prefix()
        {
            MsgIndex = EClass.game.log.currentLogIndex;
            return NetSession.Instance.IsHost;
        }

        internal static void Postfix()
        {
            if (NetSession.Instance.Connection is not ElinNetHost host) {
                return;
            }

            if (MsgIndex == EClass.game.log.currentLogIndex) {
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