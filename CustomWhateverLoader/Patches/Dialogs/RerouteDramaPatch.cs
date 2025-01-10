using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class RerouteDramaPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara.ShowDialog), [])]
    internal static IEnumerable<CodeInstruction> OnSwitchIdIl(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(
                    typeof(Card),
                    nameof(Card.id))),
                new(OpCodes.Stloc_S),
                new(OpCodes.Ldloc_S))
            .CreateLabel(out var label)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryRerouteDialog),
                new(OpCodes.Brfalse, label),
                new(OpCodes.Ret))
            .InstructionEnumeration();
    }

    // TODO: this only reroutes if it's actually going to show
    // subject to change for dialog expansion
    private static bool TryRerouteDialog(Chara chara)
    {
        if (!CustomChara.DramaRoute.TryGetValue(chara.id, out var drama)) {
            return false;
        }

        chara.ShowDialog(drama);
        return true;
    }
}