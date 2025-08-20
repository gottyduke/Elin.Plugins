using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using HarmonyLib;

namespace Cwl.Patches.UIs;

[HarmonyPatch]
internal class SkipProgressTweenPatch
{
    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(PopManager), nameof(PopManager.Pop)),
            AccessTools.Method(typeof(PopManager), "_Kill"),
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnPopTweenIl(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .End()
            .MatchStartBackwards(
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Add),
                new OpCodeContains(nameof(OpCodes.Stloc)))
            .EnsureValid("load tweener")
            .CreateLabel(out var @continue)
            .MatchEndBackwards(
                new(OpCodes.Ldarg_0),
                new OperandContains(OpCodes.Ldfld, nameof(PopManager.items)),
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new OperandContains(OpCodes.Callvirt, "Item"),
                new OperandContains(OpCodes.Call, nameof(ClassExtension.Rect)))
            .EnsureValid("get rect")
            .CreateLabel(out var proceed)
            .InsertAndAdvance(
                new(OpCodes.Dup),
                Transpilers.EmitDelegate(ShouldSkipTween),
                new(OpCodes.Brfalse, proceed),
                new(OpCodes.Pop),
                new(OpCodes.Br, @continue))
            .InstructionEnumeration();
    }

    private static bool ShouldSkipTween(PopItem item)
    {
        return item.GetComponent<ProgressIndicator>() != null;
    }
}