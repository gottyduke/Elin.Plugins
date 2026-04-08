using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ACS.API;
using Cwl.Helper.Extensions;
using HarmonyLib;
using UnityEngine;

namespace ACS.Patches;

[HarmonyPatch]
internal class RenderFramePatch
{
    private static bool _shouldRender;
    private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

    internal static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardActor), nameof(CardActor.OnRender));
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnRenderMpb(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        return cm
            .End()
            .MatchStartBackwards(
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new OperandContains(OpCodes.Ldfld, nameof(SpriteData.frame)),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Cgt))
            .EnsureValid("replace sprite data")
            .Advance(1)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                Transpilers.EmitDelegate(GetCurrentAnimatedData),
                new(OpCodes.Dup),
                new(OpCodes.Stloc_S, cm.InstructionAt(-1).operand))
            .InstructionEnumeration();
    }

    [HarmonyPostfix]
    internal static void OnRenderMbp(CardActor __instance)
    {
        if (!_shouldRender) {
            return;
        }

        _shouldRender = false;
        __instance.mpb.SetTexture(_mainTex, __instance.sr.sprite.texture);
    }

    private static SpriteData GetCurrentAnimatedData(SpriteData current, CardActor actor, RenderParam p)
    {
        _shouldRender = false;

        var owner = actor.owner;
        if (!owner.sourceCard.replacer.suffixes.ContainsKey(AcsController.ReservedSuffix)) {
            return current;
        }

        if (owner is Chara chara) {
            var data = chara.GetAcsClip(null, p.snow);
            if (data is not null) {
                _shouldRender = true;
                return data;
            }
        }

        return current;
    }
}