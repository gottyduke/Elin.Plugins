using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
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
            .EnsureValid("CardRenderer.Say")
            // preserve labels in TalkTopic
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_0).WithLabels(cm.Labels))
            .InsertAndAdvance(
                Transpilers.EmitDelegate(SetSceneTrigger))
            .InstructionEnumeration();
    }

    internal static void SetSceneTrigger(CardRenderer renderer, string text, Color color, float duration, Card card)
    {
        if (card is not Chara chara) {
            return;
        }

        if (!chara.IsGlobal) {
            // let non gc talk
            renderer.Say(text, color, duration);
        } else if (!chara.Profile.CanTalk) {
            if (!EmConfig.Scene.BlockGlobalTalk.Value) {
                renderer.Say(text, color, duration);
            }
        } else {
            EmScheduler.OnTalkTrigger(new() {
                Chara = chara,
                Trigger = text,
            });
            return;
        }

        RecentActionContext.Add($"{chara.NameSimple}: {text}");
    }
}