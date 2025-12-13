using System.Collections;
using System.ComponentModel;
using System.IO;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
    /// <summary>
    ///     Ensures scripting is enabled for this user
    /// </summary>
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

    /// <summary>
    ///     Evaluate a script string with state if there's any active
    /// </summary>
    [ConsoleCommand("eval")]
    [Description("reflex_greedy_args")]
    public static string EvaluateScript(string script)
    {
        _activeStates.TryPeek(out var activeState);

        var result = script.ExecuteAsCs(useState: activeState);

        switch (result) {
            case null:
                return "null or void";
            case IEnumerable enumerable: {
                using var sb = StringBuilderPool.Get();
                foreach (var item in enumerable) {
                    sb.AppendLine(item.ToString());
                }

                return sb.ToString();
            }
            default:
                return result.ToString();
        }
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

    private class CwlScriptState(ScriptState<object> csharpState)
    {
        public ScriptState<object> CSharpState
        {
            get => field ??= csharpState;
            set {
                if (!Pinned || field is null) {
                    field = value;
                }
            }
        }

        public bool Pinned { get; set; }
    }

    extension(string script)
    {
        /// <summary>
        ///     Run a cs script block, expensive, can't GC, bad
        /// </summary>
        /// <remarks>Do not create types here</remarks>
        public object ExecuteAsCs(object? globals = null, string? useState = null)
        {
            TestIfScriptAvailable();

            if (useState is null) {
                _activeStates.TryPeek(out useState);
            }

            if (useState is not null && _scriptStates.TryGetValue(useState, out var stateInfo)) {
                var continueState = stateInfo.CSharpState
                    .ContinueWithAsync(script, DefaultScriptOptions, ExceptionProfile.ScriptExceptionHandler,
                        UniTasklet.GameToken)
                    .GetAwaiter()
                    .GetResult();

                // may be pinned
                stateInfo.CSharpState = continueState;

                return continueState.ReturnValue;
            }

            var (_, csharp) = CompileScript(script, DefaultScriptOptions, true, globals);
            var state = csharp
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