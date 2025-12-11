using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using Cwl.API.Drama;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch]
internal class RerouteDramaPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara.ShowDialog), [])]
    internal static IEnumerable<CodeInstruction> OnSwitchIdIl(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new(OpCodes.Ldarg_0),
                new OperandContains(OpCodes.Ldfld, nameof(Card.id)),
                new(OpCodes.Stloc_S),
                new(OpCodes.Ldloc_S))
            .EnsureValid("load card id")
            .CreateLabel(out var label)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryRerouteDialog),
                new(OpCodes.Brfalse, label),
                new(OpCodes.Ret))
            .InstructionEnumeration();
    }

    // TODO: this only reroutes if it's actually going to show
    // subject to change for drama expansion
    private static bool TryRerouteDialog(Chara chara)
    {
        DramaExpansion.Clear();

        if (!CustomChara.DramaRoutes.TryGetValue(chara.id, out var drama) &&
            !chara.mapStr.TryGetValue("drama_route", out drama)) {
            return false;
        }

        try {
            chara.ShowDialog(drama);
        } catch {
            ELayerCleanup.Cleanup<LayerDrama>();
            chara.ShowDialog(chara.id);
            // noexcept
        }

        return true;
    }
}