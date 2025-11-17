#if DEBUG
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class NoActiveGCPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Core), nameof(Core.OnApplicationFocus))]
    internal static IEnumerable<CodeInstruction> OnManualGCDisposeIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new OperandContains(OpCodes.Call, nameof(Resources.UnloadUnusedAssets)),
                new(OpCodes.Pop),
                new OperandContains(OpCodes.Call, nameof(GC.Collect)))
            .RemoveInstructions(3)
            .InstructionEnumeration();
    }
}
#endif