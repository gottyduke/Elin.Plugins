using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using ElinTogether.Helper;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class RemoteHoldThingyPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CharaActorPCC), nameof(CharaActorPCC.OnRender))]
    internal static IEnumerable<CodeInstruction> OnRender105gunHandsIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Stloc_S, nameof(Thing)),
                new OperandContains(OpCodes.Ldloc_S, nameof(Thing)),
                new(OpCodes.Brfalse))
            .EnsureValid("check main hand")
            .Advance(-1)
            .SetOpcodeAndAdvance(OpCodes.Ldloca)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(GetRemoteMainHand))
            .MatchEndForward(
                new OperandContains(OpCodes.Ldfld, nameof(CharaBody.slotOffHand)),
                new OperandContains(OpCodes.Ldfld, nameof(BodySlot.thing)),
                new OperandContains(OpCodes.Stloc_S, nameof(Thing)),
                new OperandContains(OpCodes.Ldloc_S, nameof(Thing)),
                new(OpCodes.Brfalse))
            .EnsureValid("check off hand")
            .Advance(-1)
            .SetOpcodeAndAdvance(OpCodes.Ldloca)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(GetRemoteOffHand))
            .InstructionEnumeration();
    }

    private static Thing? GetRemoteMainHand(ref Thing? current, CharaActorPCC pcc)
    {
        var chara = pcc.owner;
        if (chara.IsPC) {
            return current;
        }

        if (chara.NetProfile.RemoteMainHand.TryGetTarget(out var thing) && thing is not null) {
            current = thing;
        }

        return current;
    }

    private static Thing? GetRemoteOffHand(ref Thing? current, CharaActorPCC pcc)
    {
        var chara = pcc.owner;
        if (chara.IsPC) {
            return current;
        }

        if (chara.NetProfile.RemoteOffHand.TryGetTarget(out var thing) && thing is not null) {
            current = thing;
        }

        return current;
    }
}