using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Drama;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Dramas;

[HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
internal class DramaExpansionPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.ExpandedActions;
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnParseActionIl(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new OperandContains(OpCodes.Ldfld, "action"),
                new(OpCodes.Stloc_S))
            .EnsureValid("load action")
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

    [HarmonyFinalizer]
    internal static Exception? RethrowParseLine(Exception? __exception)
    {
        if (__exception is not null && DramaExpansion.Cookie is { } cookie) {
            __exception = new DramaParseLineException(cookie, __exception);
        }

        return __exception;
    }

    [SwallowExceptions]
    private static bool ExternalInvoke(DramaManager __instance, Dictionary<string, string> item)
    {
        //! cookie must be set first to share parse state between patches
        DramaExpansion.Cookie = new(__instance, item);

        if (!item.TryGetValue("action", out var action) ||
            action is not ("invoke*" or "inject" or "i*")) {
            return false;
        }

        if (!item.TryGetValue("param", out var rawExpr)) {
            return false;
        }

        if (action == "inject") {
            if (rawExpr == "Unique") {
                DramaExpansion.InjectUniqueRumor();
            }

            return false;
        }

        // default actor
        item["actor"] = item["actor"].IsEmpty("tg");

        foreach (var expr in rawExpr.SplitLines()) {
            if (DramaExpansion.BuildExpression(expr) is not { } func) {
                continue;
            }

            var step = new DramaEventMethod(() => func(__instance, item));
            if (item.TryGetValue("jump", out var jump) && !jump.IsEmpty()) {
                step.action = null;
                step.jumpFunc = () => func(__instance, item) ? jump : "";
            }

            __instance.AddEvent(step);
        }

        return true;
    }
}