using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class ShowDialogPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara.ShowDialog), [])]
    internal static IEnumerable<CodeInstruction> OnSwitchIdIl(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(
                    typeof(Card),
                    nameof(Card.id))),
                new CodeMatch(OpCodes.Stloc_S),
                new CodeMatch(OpCodes.Ldloc_S))
            .CreateLabel(out var label)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryRerouteDialog),
                new CodeInstruction(OpCodes.Brfalse, label),
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
    }

    private static bool TryRerouteDialog(Chara chara)
    {
        if (!CustomChara.DramaRoute.TryGetValue(chara.id, out var drama)) {
            return false;
        }

        chara.ShowDialog(drama);
        return true;
    }
}