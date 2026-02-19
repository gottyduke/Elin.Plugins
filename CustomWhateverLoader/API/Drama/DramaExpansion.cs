using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
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

    internal static readonly Dictionary<string, Func<DramaManager, Dictionary<string, string>, bool>> DramaActionHandlers = [];

    public static ActionCookie? Cookie { get; internal set; }

    public static ExcelData? CurrentData =>
        DramaManager.dictCache.GetValueOrDefault($"{CorePath.DramaData}{Cookie?.Dm.setup.book}.xlsx");

    public static string CurrentState => $"{DramaScriptState}.{Cookie?.Dm.tg?.chara?.uid}";

    [ConsoleCommand("reset_states")]
    public static void ResetStates()
    {
        if (CwlScriptLoader.ActiveState == CurrentState) {
            return;
        }

        _valueStack.Clear();
        CwlScriptLoader.RemoveState(CurrentState);
    }

    public static void AddActionHandler(string action, Func<DramaManager, Dictionary<string, string>, bool> process)
    {
        DramaActionHandlers[action] = SafeProcess;
        return;

        bool SafeProcess(DramaManager dm, Dictionary<string, string> line)
        {
            try {
                return process(dm, line);
            } catch (Exception ex) {
                var exp = ExceptionProfile.GetFromStackTrace(ref ex);
                exp.Analyze();
                exp.CreateAndPop("cwl_warn_drama_play_ex".Loc($"{ex.GetType().Name}: {ex.Message}"));
                CwlMod.Log(ex);
                // noexcept
            }

            return false;
        }
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlDramaAction action)
    {
        AddActionHandler(action.Action, (dm, line) => method.FastInvokeStatic(dm, line) is true);
        CwlMod.Log<GameIOProcessor.GameIOContext>("cwl_log_processor_add".Loc("drama_action", action.Action,
            method.GetAssemblyDetail(false)));
    }

    internal static bool ProcessAction(string action)
    {
        return DramaActionHandlers.TryGetValue(action, out var handler) && handler(Cookie!.Dm, Cookie.Line);
    }

    [CwlDramaAction("inject")]
    private static bool ProcessInjectAction(DramaManager dm, Dictionary<string, string> line)
    {
        if (line["param"] == "Unique") {
            InjectUniqueRumor(dm);
        }

        return false;
    }

    [CwlDramaAction(nameof(choice))]
    private static bool ProcessConditionalChoice(DramaManager dm, Dictionary<string, string> line)
    {
        var expr = line["param"];
        if (expr.IsEmptyOrNull) {
            return false;
        }

        var func = BuildExpression(expr);
        if (func is null) {
            return false;
        }

        // add first, but conditionally remove it in invoke call
        Dictionary<string, string> choiceLine = new(line, StringComparer.Ordinal) {
            ["param"] = "",
        };

        var choices = dm.lastTalk.choices;
        var lastChoice = choices.Count;
        dm.ParseLine(choiceLine);

        if (choices.Count == lastChoice) {
            // disabled via if / if2 or failed to add or something
            return false;
        }

        choices[^1].activeCondition = () => func(dm, line);
        return true;
    }

    // always handle this action
    [CwlDramaAction("i*")]
    [CwlDramaAction("invoke*")]
    private static bool ProcessInvokeAction(DramaManager dm, Dictionary<string, string> line)
    {
        var rawExpr = line["param"];
        if (rawExpr.StartsWith("//")) {
            return true;
        }

        // TODO: maybe allow multiline params?
        foreach (var expr in rawExpr.SplitLines()) {
            if (BuildExpression(expr) is not { } func) {
                continue;
            }

            // for old i* style usage
            if (expr.StartsWith(nameof(choice))) {
                Dictionary<string, string> choiceLine = new(line, StringComparer.Ordinal) {
                    ["action"] = nameof(choice),
                    ["param"] = "", // reset so we don't use new action handler
                };

                var lastChoice = dm.lastTalk.choices.Count;
                dm.ParseLine(choiceLine);

                if (dm.lastTalk.choices.Count == lastChoice) {
                    // disabled via if / if2 or failed to add or something
                    return true;
                }

                dm.lastTalk.choices[^1].activeCondition = () => func(dm, line);
            }

            var jump = line["jump"];
            var method = new DramaEventMethod(() => func(dm, line));

            if (!jump.IsEmptyOrNull) {
                method.action = null;
                method.jumpFunc = () => func(dm, line) ? jump : "";
            }

            dm.AddEvent(method);
        }

        return true;
    }

    // always handle this action
    [CwlDramaAction(nameof(eval))]
    private static bool ProcessEvalAction(DramaManager dm, Dictionary<string, string> line)
    {
        var expr = line["param"];
        if (expr.IsEmptyOrNull) {
            return true;
        }

        var submission = CwlScriptSubmission.Create(dm.setup.book);
        var jump = line["jump"];
        var state = new DramaScriptState {
            dm = dm,
            line = line,
        };

        DramaEventMethod method = jump.IsEmptyOrNull
            ? new(() => DeferredCompileAndRun())
            : new(null) {
                jumpFunc = () => DeferredCompileAndRun() is true ? jump : "",
            };

        dm.AddEvent(method);

        return true;

        // defer compilation until executed
        object? DeferredCompileAndRun()
        {
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

            var csharp = submission.CompileAndRun<DramaScriptState>(expr);
            return csharp?.Invoke(state);
        }
    }
}