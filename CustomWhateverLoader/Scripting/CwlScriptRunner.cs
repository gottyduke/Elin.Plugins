using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Unity;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
    private static readonly Dictionary<string, CwlScriptState> _scriptStates = [];

    internal static ScriptOptions DefaultScriptOptions =>
        field ??= ScriptOptions.Default
            .WithReferences(CurrentDomainReferences)
            .WithImports(CurrentDomainNamespaces);

    public static void TestIfScriptAvailable()
    {
        if (!CwlMod.LoadingComplete) {
            throw new ScriptLoaderNotReadyException();
        }

        if (!CwlConfig.AllowScripting) {
            throw new ScriptDisabledException();
        }

        // add some other runtime feature set checks
    }

    [ConsoleCommand("clear_state")]
    public static void ClearState(string state = "shared")
    {
        if (_scriptStates.Remove(state)) {
            CwlMod.Popup<ScriptState>("cwl_ui_cs_state_remove".lang());
        }
    }

    [ConsoleCommand("pin_state")]
    public static void PinState(string state = "shared", bool pinned = true)
    {
        if (_scriptStates.TryGetValue(state, out var scriptState)) {
            scriptState.Pinned = pinned;
        }
    }

    [ConsoleCommand("eval")]
    [Description("reflex_greedy_args")]
    public static string EvaluateScript(string script)
    {
        return $"{script.ExecuteAsCs()}";
    }

    [ConsoleCommand("file")]
    [Description("reflex_greedy_args")] // path may contain spaces
    public static string EvaluateFile(string filePath)
    {
        if (!File.Exists(filePath)) {
            return "cwl_error_cs_file_not_found".lang();
        }

        return EvaluateScript(File.ReadAllText(filePath));
    }

    [ConsoleCommand("interactive")]
    public static string EvaluateInteractive()
    {
        return "";
    }

    internal static void InitState()
    {
    }

    private class CwlScriptState(ScriptState<object> scriptState)
    {
        public ScriptState<object> ScriptState
        {
            get => field ??= scriptState;
            set {
                if (!Pinned || field is null) {
                    field = value;
                }
            }
        }

        public bool Pinned { get; set; }
    }

    extension(string scriptStr)
    {
        /// <summary>
        ///     Run a cs script block, expensive, can't GC, bad
        /// </summary>
        /// <remarks>Do not create types here</remarks>
        public object ExecuteAsCs(object? globals = null, string? useState = null)
        {
            TestIfScriptAvailable();

            if (useState is not null && _scriptStates.TryGetValue(useState, out var stateInfo)) {
                var continueState = stateInfo.ScriptState
                    .ContinueWithAsync(scriptStr, DefaultScriptOptions, ExceptionProfile.ScriptExceptionHandler,
                        UniTasklet.GameToken)
                    .GetAwaiter()
                    .GetResult();

                // may be pinned
                stateInfo.ScriptState = continueState;

                return continueState.ReturnValue;
            }

            var (_, script) = CompileScript(scriptStr, DefaultScriptOptions, true, globals);
            var state = script
                .RunAsync(globals, ExceptionProfile.ScriptExceptionHandler, UniTasklet.GameToken)
                .GetAwaiter()
                .GetResult();

            if (useState is not null && state.Exception is null) {
                _scriptStates[useState] = new(state);
            }

            return state.ReturnValue;
        }
    }
}