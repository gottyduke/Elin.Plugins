using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.Scripting;
using MethodTimer;
using ReflexCLI.Attributes;

namespace Cwl.API.Drama;

[ConsoleCommandClassCustomizer("cwl.dm")]
public partial class DramaExpansion : DramaOutcome
{
    public const string DramaScriptState = "drama";

    internal static readonly Dictionary<string, Action<DramaManager, Dictionary<string, string>>> DramaActionHandlers = [];

    public static ActionCookie? Cookie { get; internal set; }

    public static ExcelData? CurrentData =>
        DramaManager.dictCache.GetValueOrDefault($"{CorePath.DramaData}{Cookie?.Dm.setup.book}.xlsx");

    [ConsoleCommand("reset_states")]
    public static void ResetStates()
    {
        _valueStack.Clear();
        CwlScriptLoader.RemoveState(DramaScriptState);
    }

    public static void Add(string action, Action<DramaManager, Dictionary<string, string>> process)
    {
        DramaActionHandlers[action] = process;
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlDramaAction action)
    {
        Add(action.Action, (dm, line) => method.FastInvokeStatic(dm, line));
        CwlMod.Log<GameIOProcessor.GameIOContext>("cwl_log_processor_add".Loc("drama_action", action.Action,
            method.GetAssemblyDetail(false)));
    }

    internal static bool ProcessAction(string action)
    {
        if (!DramaActionHandlers.TryGetValue(action, out var handler)) {
            return false;
        }

        handler(Cookie!.Dm, Cookie.Line);
        return true;
    }

    [CwlDramaAction("inject")]
    private static void ProcessInjectAction(DramaManager dm, Dictionary<string, string> line)
    {
        if (line["param"] == "Unique") {
            InjectUniqueRumor(dm);
        }
    }

    [CwlDramaAction("i*")]
    [CwlDramaAction("invoke*")]
    private static void ProcessInvokeAction(DramaManager dm, Dictionary<string, string> line)
    {
        var rawExpr = line["param"];
        if (rawExpr.StartsWith("//")) {
            return;
        }

        // TODO: maybe allow multiline params?
        foreach (var expr in rawExpr.SplitLines()) {
            if (BuildExpression(expr) is not { } func) {
                continue;
            }

            if (expr.StartsWith(nameof(choice))) {
                func(dm, line);
                continue;
            }

            var step = new DramaEventMethod(() => func(dm, line));
            if (line.TryGetValue("jump", out var jump) && !jump.IsEmptyOrNull) {
                step.action = null;
                step.jumpFunc = () => func(dm, line) ? jump : "";
            }

            dm.AddEvent(step);
        }
    }

    [CwlDramaAction("eval")]
    private static void ProcessEvalAction(DramaManager dm, Dictionary<string, string> line)
    {
        var expr = line["param"];
        // import
        if (expr.StartsWith("<<<")) {
            var scriptFile = expr[3..].Trim();
            var root = Path.GetDirectoryName(CurrentData!.path)!;
            var filePath = Path.Combine(root, scriptFile);

            if (!File.Exists(filePath)) {
                throw new FileNotFoundException(scriptFile);
            }

            expr = File.ReadAllText(filePath);
        }

        expr.ExecuteAsCs(new { dm }, DramaScriptState);
    }
}