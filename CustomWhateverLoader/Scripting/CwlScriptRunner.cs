using System.ComponentModel;
using System.IO;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
    /// <summary>
    ///     Ensures scripting is enabled for this user
    /// </summary>
    [ConsoleCommand("is_ready")]
    public static string TestIfScriptAvailable()
    {
        if (!CwlMod.LoadingComplete) {
            throw new ScriptLoaderNotReadyException();
        }

        if (!CwlConfig.AllowScripting) {
            throw new ScriptDisabledException();
        }

        // add some other runtime feature set checks

        return "cwl_ui_cs_ready".lang();
    }

    /// <summary>
    ///     Evaluate a script string with state if there's any active
    /// </summary>
    [ConsoleCommand("eval")]
    [Description("reflex_greedy_args")]
    public static string EvaluateScript(string script)
    {
        _activeStates.TryPeek(out var activeState);

        // caching console commands are usually not worth it
        var result = script.ExecuteAsCs(useState: activeState, useCache: false);
        return result.TryToString("null or void");
    }

    [ConsoleCommand("file")]
    [Description("reflex_greedy_args")] // path may contain spaces
    public static string EvaluateFile(string filePath)
    {
        return EvaluateScript(File.ReadAllText(filePath));
    }

    extension(string script)
    {
        /// <summary>
        ///     Run a cs script block, expensive, can't GC, bad
        /// </summary>
        /// <remarks>Do not create types here</remarks>
        public object ExecuteAsCs(object? globals = null, string? useState = null, bool useCache = true)
        {
            TestIfScriptAvailable();

            if (useState is null) {
                _activeStates.TryPeek(out useState);
            }

            if (useState is null || !_scriptStates.TryGetValue(useState, out var scriptState)) {
                scriptState = new();
            }

            // TODO: box value types
            var globalVars = globals.TokenizeObject();
            foreach (var (key, value) in globalVars) {
                scriptState[key] = value;
            }

            var runner = CompileScriptRunner(script, DefaultScriptOptions, useCache, true);
            var state = runner(scriptState)
                .GetAwaiter()
                .GetResult();

            if (useState is not null) {
                _scriptStates[useState] = scriptState;
            }

            return state;
        }
    }
}