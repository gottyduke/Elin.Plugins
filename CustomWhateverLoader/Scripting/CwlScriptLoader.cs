#if CWL_SCRIPTING
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

[ConsoleCommandClassCustomizer("cwl.cs")]
public static class CwlScriptLoader
{
    private static ScriptState<object>? _sharedState;
    private static bool _pinnedSharedState;
    private static IReadOnlyList<MetadataReference> CurrentDomainReferences => field ??= CreateStaticDomainReferences();
    private static Dictionary<BaseModPackage, CompilationReference> UserAssemblies => field ??= [];

    [ConsoleCommand("clear_shared_state")]
    public static void ClearSharedState()
    {
        _sharedState = null;
    }

    [ConsoleCommand("pin_shared_state")]
    public static void PinSharedState(bool pinned = true)
    {
        _pinnedSharedState = pinned;
    }

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

    internal static (Compilation compilation, Script<object> script) CompileScript(string scriptStr,
                                                                                   ScriptOptions options,
                                                                                   bool throwOnFailure = false,
                                                                                   object? globals = null)
    {
        TestIfScriptAvailable();

        var script = CSharpScript.Create(scriptStr, options, globals?.GetType());
        var compilation = script.GetCompilation();

        if (throwOnFailure) {
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Join(d => d.GetMessage());
            if (!errors.IsEmptyOrNull) {
                throw new ScriptCompilationException(errors);
            }
        }

        return (compilation, script);
    }

    internal static Compilation CompileScriptFromDir(DirectoryInfo scriptDir)
    {
        TestIfScriptAvailable();

        if (!scriptDir.Exists) {
            throw new DirectoryNotFoundException($"Directory not found: {scriptDir.FullName}");
        }

        var scripts = scriptDir.GetFiles("*.cs", SearchOption.TopDirectoryOnly);
        if (scripts.Length == 0) {
            throw new ScriptCompilationException("cwl_error_cs_empty_script_dir");
        }

        var trees = scripts
            .Select(f => CSharpSyntaxTree.ParseText(
                File.ReadAllText(f.FullName),
                new(kind: SourceCodeKind.Script),
                f.FullName))
            .ToList();

        return CSharpCompilation.Create(
            $"cwl-script-{scriptDir.FullName.ShortPath().GetHashCode()}",
            trees,
            CreateCurrentDomainReferences(),
            new(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable,
                optimizationLevel: OptimizationLevel.Release)
        );
    }

    internal static Compilation CompileScriptFromPackage(BaseModPackage package)
    {
        TestIfScriptAvailable();

        return CompileScriptFromDir(package.dirInfo);
    }

    // expensive
    private static List<MetadataReference> CreateStaticDomainReferences()
    {
        // this is a dynamic image but necessary to reference
        var unityImage = Path.Combine(CorePath.rootExe, "Elin_Data/Managed/UnityEngine.CoreModule.dll");

        List<MetadataReference> references = [
            MetadataReference.CreateFromFile(unityImage),
        ];

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies) {
            try {
                if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)) {
                    continue;
                }

                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                // noexcept
            }
        }

        return references;
    }

    private static IReadOnlyList<MetadataReference> CreateCurrentDomainReferences()
    {
        return [
            ..CurrentDomainReferences,
            ..UserAssemblies.Values,
        ];
    }

    extension(string scriptStr)
    {
        /// <summary>
        ///     Run a cs script block, expensive, can't GC, bad
        /// </summary>
        /// <remarks>Do not create types here</remarks>
        public object ExecuteAsCs(object? globals = null, bool sharedState = false)
        {
            var options = ScriptOptions.Default.WithReferences(CreateCurrentDomainReferences());

            if (sharedState && _sharedState is not null) {
                var continueState = _sharedState
                    .ContinueWithAsync(scriptStr, options, ExceptionProfile.ScriptExceptionHandler, UniTasklet.GameToken)
                    .GetAwaiter()
                    .GetResult();

                if (!_pinnedSharedState) {
                    _sharedState = continueState;
                }

                return continueState.ReturnValue;
            }

            var (_, script) = CompileScript(scriptStr, options, true, globals);
            var state = script.RunAsync(globals, ExceptionProfile.ScriptExceptionHandler, UniTasklet.GameToken)
                .GetAwaiter()
                .GetResult();

            if (sharedState && state.Exception is null) {
                _sharedState = state;
            }

            return state.ReturnValue;
        }
    }
}
#endif