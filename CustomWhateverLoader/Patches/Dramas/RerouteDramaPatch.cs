using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using Cwl.API.Drama;
using Cwl.Helper.Extensions;
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
            .End()
            .MatchEndBackwards(
                new OperandContains(OpCodes.Call, nameof(Chara.ShowDialog)))
            .SetInstruction(
                Transpilers.EmitDelegate(TryRerouteDialog))
            .InstructionEnumeration();
    }

    private static LayerDrama TryRerouteDialog(Chara chara, string book, string step, string tag)
    {
        DramaExpansion.ResetStates();

        if (CustomChara.DramaRoutes.TryGetValue(chara.id, out var drama) ||
            chara.mapStr.TryGetValue("drama_route", out drama)) {
            return chara.ShowDialog(drama);
        }

        return chara.ShowDialog(book, step, tag);
    }
}