using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Emmersive.API.Profiles;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using EModding.Helper;
using HarmonyLib;
using UnityEngine;

namespace Emmersive.Patches;

[HarmonyPatch]
internal class RendererPopPatch
{
    private static (Card Card, bool Show)? _lastPop;

    private static readonly MethodInfo _rendererSay = AccessTools.Method(
        typeof(CardRenderer),
        nameof(CardRenderer.Say),
        [typeof(string), typeof(Color), typeof(float)]);

    private static readonly MethodInfo _msgSay = AccessTools.Method(
        typeof(Msg),
        nameof(Msg.Say),
        [typeof(string)]);

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            ..AccessTools.GetDeclaredMethods(typeof(Card))
                .Where(mi => mi.Name is nameof(Card.SayRaw) or nameof(Card.TalkRaw)),
            AccessTools.Method(typeof(Chara), nameof(Chara.TalkTopic)),
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnRendererPopIl(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var cm = new CodeMatcher(instructions);
        cm.MatchEndForward(
                new CodeMatch(ci => ci.Calls(_rendererSay)))
            .EnsureValid("set scene trigger on CardRenderer.Say")
            // preserve labels in TalkTopic
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_0).WithLabels(cm.Labels))
            .InsertAndAdvance(
                Transpilers.EmitDelegate(SetSceneTrigger))
            .End()
            .MatchEndBackwards(
                new CodeMatch(ci => ci.Calls(_msgSay)));

        if (cm.IsInvalid) {
            if (original.Name != nameof(Card.SayRaw)) {
                EmMod.Warn<RendererPopPatch>($"Msg.Say not found in {original.Name}, barks will not be gated");
            }

            return cm.InstructionEnumeration();
        }

        Func<string, Card, string, string> onLog = original.Name == nameof(Chara.TalkTopic)
            ? OnTopicLog
            : OnTalkLog;

        return cm
            .Repeat(m => m
                .RemoveInstruction()
                .InsertAndAdvance(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldarg_1),
                    Transpilers.EmitDelegate(onLog)))
            .InstructionEnumeration();
    }

    internal static string OnTalkLog(string text, Card card, string topic)
    {
        var show = _lastPop is not { } pop || pop.Card != card || pop.Show;
        _lastPop = null;

        return LogOrBlock(text, show);
    }

    internal static string OnTopicLog(string text, Card card, string topic)
    {
        var show = topic == "dead" || card is not Chara { Profile: { } profile } chara || CanShowPop(chara, profile);
        return LogOrBlock(text, show);
    }

    private static string LogOrBlock(string text, bool show)
    {
        if (show) {
            Msg.Say(text);
        } else {
            Msg.SetColor();
        }

        return "";
    }

    internal static void SetSceneTrigger(CardRenderer renderer, string text, Color color, float duration, Card card)
    {
        if (card is not Chara { Profile: { } profile } chara) {
            renderer.Say(text, color, duration);
            return;
        }

        text = chara.ApplyNewLine(text).StripBrackets();

        var show = CanShowPop(chara, profile);
        _lastPop = (card, show);

        if (!show) {
            EmMod.Debug<RendererPopPatch>($"blocked {chara.NameSimple}");
            return;
        }

        if (IsSceneCandidate(chara, profile) &&
            (!profile.LockedInRequest || profile.IsPC) &&
            EmScheduler.CanMakeRequest) {
            EmScheduler.OnTalkTrigger(new() {
                Chara = chara,
                Trigger = text,
                AlreadyShown = true,
            });
            EmScheduler.AddBufferDelay(EmConfig.Scene.SceneBufferWindow.Value);
        }

        renderer.Say(text, color, duration);
        profile.ResetTalkCooldown();
        MemoryManager.Instance.RecordTalk(chara, text);
    }

    private static bool IsSceneCandidate(Chara chara, CharaProfile profile)
    {
        return EmScheduler.IsEnabled &&
               ApiPoolSelector.Instance.HasAnyAvailableServices() &&
               profile.CanTrigger &&
               chara.Dist(EClass.pc) <= EmConfig.Context.NearbyRadius.Value;
    }

    private static bool CanShowPop(Chara chara, CharaProfile profile)
    {
        if (profile.IsPC || !IsSceneCandidate(chara, profile)) {
            return true;
        }

        return !profile.OnTalkCooldown && (!profile.LockedInRequest || !EmConfig.Scene.BlockCharaTalk.Value);
    }
}