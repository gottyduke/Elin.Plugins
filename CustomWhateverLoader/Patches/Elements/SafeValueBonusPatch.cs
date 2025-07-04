using System.Collections.Generic;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class SafeValueBonusPatch
{
    internal static bool Prepare()
    {
        return GameVersion.IsBelow(0, 23, 154);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ElementContainerCard), nameof(ElementContainerCard.ValueBonus))]
    internal static IEnumerable<CodeInstruction> OnReadMapCharaIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new OperandContains(OpCodes.Call, nameof(EClass._map)),
                new OperandContains(OpCodes.Ldfld, nameof(Map.charas)),
                new OperandContains(OpCodes.Callvirt, nameof(List<Chara>.GetEnumerator)),
                new OpCodeContains(nameof(OpCodes.Stloc)))
            .ThrowIfInvalid("failed to match get_map_charas")
            .RemoveInstructions(3)
            .InsertAndAdvance(
                Transpilers.EmitDelegate(SafeGetCharas))
            .InstructionEnumeration();
    }

    private static List<Chara>.Enumerator SafeGetCharas()
    {
        return (EClass.game.activeZone?.map?.charas ?? [EClass.pc]).GetEnumerator();
    }
}