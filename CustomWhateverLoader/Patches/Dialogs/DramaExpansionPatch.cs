using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Drama;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class DramaExpansionPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.ExpandedActions;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
    internal static IEnumerable<CodeInstruction> OnParseAction(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new OperandContains(OpCodes.Ldfld, "action"),
                new(OpCodes.Stloc_S))
            .CreateLabel(out var label)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                Transpilers.EmitDelegate(ExternalInvoke),
                new(OpCodes.Brfalse, label),
                new(OpCodes.Pop),
                new(OpCodes.Ret))
            .InstructionEnumeration();
    }

    [SwallowExceptions]
    private static bool ExternalInvoke(DramaManager __instance, Dictionary<string, string> item)
    {
        if (item.GetValueOrDefault("action") is not "invoke*") {
            return false;
        }

        if (!item.TryGetValue("param", out var expr) || expr.IsEmpty()) {
            return false;
        }

        var action = DramaExpansion.BuildExpression(expr);
        if (action is null) {
            return false;
        }

        DramaExpansion.Cookie = new(__instance, item);

        var step = new DramaEventMethod(() => action(__instance, item));
        if (item.TryGetValue("jump", out var jump)) {
            step.action = null;
            step.jumpFunc = () => action(__instance, item) ? jump : "";
        }

        __instance.AddEvent(step);

        return true;
    }
}