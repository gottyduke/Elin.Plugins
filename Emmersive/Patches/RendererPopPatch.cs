using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Emmersive.Components;
using Emmersive.Contexts;
using Emmersive.Helper;
using HarmonyLib;
using UnityEngine;

namespace Emmersive.Patches;

[HarmonyPatch]
internal class RendererPopPatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            ..AccessTools.GetDeclaredMethods(typeof(Card))
                .Where(mi => mi.Name is nameof(Card.SayRaw) or nameof(Card.TalkRaw)),
            AccessTools.Method(typeof(Chara), nameof(Chara.TalkTopic)),
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnRendererPopIl(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        return cm
            .MatchEndForward(
                new OperandContains(OpCodes.Callvirt, nameof(CardRenderer.Say)))
            .EnsureValid("set scene trigger on CardRenderer.Say")
            // preserve labels in TalkTopic
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_0).WithLabels(cm.Labels))
            .InsertAndAdvance(
                Transpilers.EmitDelegate(SetSceneTrigger))
            .End()
            // TalkRaw & TalkTopic
            .MatchEndBackwards(
                new OperandContains(OpCodes.Call, nameof(Msg.Say)))
            .Repeat(m => m
                .RemoveInstruction()
                .InsertAndAdvance(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldarg_1),
                    Transpilers.EmitDelegate(OnBlockedTalk)))
            .InstructionEnumeration();
    }

    internal static string OnBlockedTalk(string text, Card card, string topic)
    {
        if (card is not Chara chara || topic == "dead") {
            Msg.Say(text);
            return "";
        }

        if (EmScheduler.IsInProgress) {
            // block all talks during scene request
            Msg.SetColor();
            return "";
        }

        if (!chara.Profile.CanTalk) {
            if (!EmConfig.Scene.BlockCharaTalk.Value) {
                // let non-blocked gc talk
                Msg.Say(text);
            }
        }

        Msg.SetColor();
        return "";
    }

    internal static void SetSceneTrigger(CardRenderer renderer, string text, Color color, float duration, Card card)
    {
        text = card.ApplyNewLine(text).StripBrackets();

        if (card is not Chara chara) {
            renderer.Say(text, color, duration);
            return;
        }

        if (EmScheduler.IsInProgress) {
            // block all talks during scene request
            return;
        }

        if (!chara.Profile.CanTalk) {
            if (!EmConfig.Scene.BlockCharaTalk.Value) {
                // let non-blocked gc talk
                renderer.Say(text, color, duration);
            }
        } else {
            if (EClass.pc.Profile.CanTalk) {
                EmScheduler.OnTalkTrigger(new() {
                    Chara = chara,
                    Trigger = text,
                });
            }
        }

        RecentActionContext.Add(chara.NameSimple, text);
    }
}