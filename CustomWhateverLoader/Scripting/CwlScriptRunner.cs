using System.ComponentModel;
using System.IO;
using System.Linq;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

[ConsoleCommandClassCustomizer("cwl.cs")]
public static partial class CwlScriptRunner
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
        if (!filePath.EndsWith(".cs")) {
            filePath += ".cs";
        }

        if (!Path.IsPathFullyQualified(filePath)) {
            var rootPath = Path.Combine(CorePath.rootExe, filePath);
            if (File.Exists(rootPath)) {
                filePath = rootPath;
            } else {
                filePath = PackageIterator
                    .GetFiles(Path.Combine("Exec", filePath))
                    .LastOrDefault()?.FullName ?? filePath;
            }
        }

        if (!File.Exists(filePath)) {
            return "file does not exist";
        }

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

            var runner = CwlScriptCompiler.CompileScriptRunner(script, CwlScriptOptions.DefaultScriptOptions, useCache, true);
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