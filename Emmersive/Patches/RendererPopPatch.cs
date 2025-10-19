using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Emmersive.API.Services;
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
        if (card is not Chara chara || topic == "dead" || !ApiPoolSelector.Instance.HasAnyAvailableServices()) {
            AllowOriginalText();
            return "";
        }

        if (EmScheduler.IsInProgress) {
            // block all talks during scene request
            Msg.SetColor();
            return "";
        }

        var profile = chara.Profile;
        if (profile.LockedInRequest) {
            if (!EmConfig.Scene.BlockCharaTalk.Value && !profile.OnTalkCooldown) {
                // let non-blocked gc talk
                AllowOriginalText();
            }
        }

        Msg.SetColor();
        return "";

        void AllowOriginalText()
        {
            Msg.Say(text);
            RecentActionContext.Add(card.NameSimple, text);
        }
    }

    internal static void SetSceneTrigger(CardRenderer renderer, string text, Color color, float duration, Chara chara)
    {
        var profile = chara.Profile;

        text = chara.ApplyNewLine(text).StripBrackets();

        if (!ApiPoolSelector.Instance.HasAnyAvailableServices() ||
            chara.Dist(EClass.pc) > EmConfig.Context.NearbyRadius.Value) {
            AllowOriginalPop();
            return;
        }

        // block if chara is locked in scheduler
        if (profile.LockedInRequest) {
            if (!EmConfig.Scene.BlockCharaTalk.Value && !profile.OnTalkCooldown) {
                AllowOriginalPop();
            } else {
                EmMod.DebugPopup<EmScheduler>($"blocked {chara.NameSimple}");
            }

            return;
        }

        if (!EmScheduler.CanMakeRequest && !profile.OnTalkCooldown) {
            AllowOriginalPop();
            return;
        }

        // make a new trigger
        EmScheduler.OnTalkTrigger(new() {
            Chara = chara,
            Trigger = text,
        });

        EmScheduler.AddBufferDelay(EmConfig.Scene.SceneBufferWindow.Value);

        return;

        void AllowOriginalPop()
        {
            renderer.Say(text, color, duration);
            profile.ResetTalkCooldown();
            profile.LastTalk = text;
            RecentActionContext.Add(chara.NameSimple, text);
        }
    }
}