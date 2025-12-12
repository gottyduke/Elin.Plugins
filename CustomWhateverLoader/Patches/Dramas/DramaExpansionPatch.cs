using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using Cwl.API.Drama;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.Scripting;
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
        if (__exception is not null && DramaExpansion.Cookie is not null) {
            __exception = new DramaParseLineException(__exception);
        }

        return __exception;
    }

    [SwallowExceptions]
    private static bool ExternalInvoke(DramaManager __instance, Dictionary<string, string> item)
    {
        //! cookie must be set first to share parse state between patches
        DramaExpansion.Cookie = new(__instance, item);

        if (!item.TryGetValue("action", out var action)) {
            return false;
        }

        if (!item.TryGetValue("param", out var rawExpr)) {
            return false;
        }

        // set default actor
        item["actor"] = item["actor"].OrIfEmpty("tg");

        switch (action.Trim()) {
            case "i*":
            case "invoke*":
                ProcessInvokeAction();
                break;
            case "inject":
                ProcessInjectAction();
                break;
            case "eval":
                ProcessEvalAction();
                break;
            default:
                return false;
        }

        return true;

        void ProcessInjectAction()
        {
            if (rawExpr == "Unique") {
                DramaExpansion.InjectUniqueRumor();
            }
        }

        void ProcessInvokeAction()
        {
            // TODO: maybe allow multiline params?
            foreach (var expr in rawExpr.SplitLines()) {
                if (DramaExpansion.BuildExpression(expr) is not { } func) {
                    continue;
                }

                if (expr.StartsWith(nameof(DramaExpansion.choice))) {
                    func(__instance, item);
                    continue;
                }

                var step = new DramaEventMethod(() => func(__instance, item));
                if (item.TryGetValue("jump", out var jump) && !jump.IsEmptyOrNull) {
                    step.action = null;
                    step.jumpFunc = () => func(__instance, item) ? jump : "";
                }

                __instance.AddEvent(step);
            }
        }

        void ProcessEvalAction()
        {
            // import
            if (rawExpr.StartsWith("<<<")) {
                var scriptFile = rawExpr[3..].Trim();
                var root = Path.GetDirectoryName(DramaExpansion.CurrentData!.path)!;
                var filePath = Path.Combine(root, scriptFile);

                if (!File.Exists(filePath)) {
                    throw new FileNotFoundException(scriptFile);
                }

                rawExpr = File.ReadAllText(filePath);
            }

            rawExpr.ExecuteAsCs(new { dm = __instance }, "drama");
        }
    }
}