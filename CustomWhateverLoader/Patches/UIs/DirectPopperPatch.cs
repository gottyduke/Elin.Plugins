using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.UIs;

[HarmonyPatch]
internal class DirectPopperPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIContextMenuPopper), nameof(UIContextMenuPopper.Pop))]
    internal static IEnumerable<CodeInstruction> OnHideAllIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new OperandContains(OpCodes.Ldsfld, nameof(UIContextMenu.Current)),
                new OperandContains(OpCodes.Callvirt, nameof(Component.GetComponentsInChildren)))
            .RemoveInstruction()
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(GetImmediateParent))
            .InstructionEnumeration();
    }

    private static UIContextMenu GetImmediateParent(UIContextMenuPopper popper)
    {
        return popper.parent == null ? UIContextMenu.Current : popper.parent;
    }
}