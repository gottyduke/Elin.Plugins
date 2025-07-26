using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Effects;

[HarmonyPatch]
internal class LaserByTraitPatch
{
    private static bool _patched;

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AttackProcess), nameof(AttackProcess.PlayRangedAnime))]
    internal static IEnumerable<CodeInstruction> OnDelayEffectIl(IEnumerable<CodeInstruction> instructions)
    {
        if (_patched) {
            return instructions;
        }

        _patched = true;

        var cm = new CodeMatcher(instructions)
            .MatchEndForward(
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new OperandContains(OpCodes.Ldftn, nameof(AttackProcess.PlayRangedAnime)));

        if (!cm.IsValid || cm.Operand is not MethodBase functor) {
            CwlMod.Warn<LaserByTraitPatch>("failed to patch rail laser check");
            return cm.InstructionEnumeration();
        }

        var harmony = new Harmony(ModInfo.Guid);
        harmony.Patch(functor, transpiler: new(typeof(LaserByTraitPatch), nameof(OnCheckRailIl)));

        return cm.InstructionEnumeration();
    }

    internal static IEnumerable<CodeInstruction> OnCheckRailIl(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        return cm
            .MatchEndForward(
                new OperandContains(OpCodes.Ldfld, nameof(AttackProcess.weapon)),
                new OperandContains(OpCodes.Ldfld, nameof(Card.id)),
                new OperandContains(OpCodes.Ldstr, "gun_rail"),
                new OperandContains(OpCodes.Call, nameof(String)),
                new(OpCodes.Brfalse))
            .InsertAndAdvance(
                new(OpCodes.Pop),
                new(OpCodes.Ldarg_0),
                cm.InstructionAt(-4),
                Transpilers.EmitDelegate(IsLaserGun))
            .InstructionEnumeration();
    }

    private static bool IsLaserGun(Thing weapon)
    {
        return weapon.trait is TraitToolRangeGunEnergy ||
               weapon.source.tag.Contains("addLaser");
    }
}