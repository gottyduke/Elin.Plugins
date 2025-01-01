using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Traits;

[HarmonyPatch]
internal class MerchantOnBarterPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
    internal static IEnumerable<CodeInstruction> OnRestockIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(
                    typeof(Trait),
                    nameof(Trait.ShopType))))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup),
                Transpilers.EmitDelegate(ShouldGenerate))
            .InstructionEnumeration();
    }

    private static void ShouldGenerate(Trait trait)
    {
        if (trait is CustomMerchant merchant) {
            merchant.Generate();
        }
    }
}