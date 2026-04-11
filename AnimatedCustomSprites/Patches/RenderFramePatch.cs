using System.Collections.Generic;
using System.Reflection.Emit;
using ACS.API;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace ACS.Patches;

[HarmonyPatch]
internal class RenderFramePatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CardActor), nameof(CardActor.OnRender))]
    internal static IEnumerable<CodeInstruction> OnRenderMpb(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        return cm
            .End()
            .MatchStartBackwards(
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new OperandContains(OpCodes.Ldfld, nameof(SpriteData.frame)),
                new(OpCodes.Ldc_I4_1))
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

    private static SpriteData GetCurrentAnimatedData(SpriteData current, CardActor actor, RenderParam p)
    {
        var owner = actor.owner;
        if (!owner.sourceCard.replacer.suffixes.ContainsKey(AcsController.ReservedSuffix)) {
            return current;
        }

        if (owner is Chara chara) {
            var data = chara.GetAcsClip(null, p.snow);
            if (data is not null) {
                return data;
            }
        }

        return current;
    }
}