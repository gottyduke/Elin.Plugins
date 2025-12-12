using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

[ConsoleCommandClassCustomizer("cwl.cs")]
public static partial class CwlScriptLoader
{
    private static readonly string[] _defaultScriptNamespaces = [
        "System",
        "System.Collections",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Reflection",
        // cwl
        "Cwl.API",
        "Cwl.API.Attributes",
        "Cwl.API.Custom",
        "Cwl.API.Drama",
        "Cwl.API.Processors",
        "Cwl.Helper",
        "Cwl.Helper.Extensions",
        "Cwl.Helper.FileUtil",
        "Cwl.Helper.Unity",
        "Cwl.Helper.Exceptions",
        "Cwl.LangMod",
        "Cwl.Scripting",
        // mods
        "HarmonyLib",
        // unity
        "UnityEngine",
        "UnityEngine.UI",
    ];

    private static readonly string[] _defaultPreprocessors = [
#if DEBUG
        "DEBUG",
#endif
        "TRACE",
#if NIGHTLY
        "NIGHTLY",
#endif
    ];

    internal static IReadOnlyList<MetadataReference> StaticDomainReferences => field ??= CreateStaticDomainReferences();
    internal static IReadOnlyList<MetadataReference> CurrentDomainReferences => [
        ..StaticDomainReferences,
        ..UserAssemblies.Values,
    ];

    internal static HashSet<string> CurrentDomainNamespaces => field ??= [.._defaultScriptNamespaces];
    internal static Dictionary<BaseModPackage, CompilationReference> UserAssemblies => field ??= [];

    internal static CSharpParseOptions DefaultParseOptions =>
        field ??= new(LanguageVersion.Latest,
            DocumentationMode.None,
            SourceCodeKind.Script,
            _defaultPreprocessors);

    internal static CSharpCompilationOptions DefaultCompilationOptions =>
        field ??= new(OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable,
            optimizationLevel: OptimizationLevel.Release);

    public static void RegisterDefaultNamespaces(Assembly assembly)
    {
        var namespaces = GetNamespaces(assembly);
        if (namespaces.Count == 0) {
            return;
        }

        CurrentDomainNamespaces.UnionWith(namespaces);

        CwlMod.Log("cwl_log_cs_add_fqdn".Loc(TypeQualifier.GetMappedAssemblyName(assembly), namespaces.Join(delimiter: ";")));
    }

    internal static (Compilation compilation, Script<object> script) CompileScript(string scriptStr,
                                                                                   ScriptOptions options,
                                                                                   bool throwOnFailure = false,
                                                                                   object? globals = null)
    {
        CwlMod.Log("cwl_log_csc_single".Loc(scriptStr.GetHashCode()));

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

    internal static Compilation? CompileScriptFromDir(DirectoryInfo scriptDir, CSharpCompilationOptions? options = null)
    {
        if (!scriptDir.Exists) {
            throw new DirectoryNotFoundException($"directory not found: {scriptDir.FullName}");
        }

        var scripts = scriptDir.GetFiles("*.cs", SearchOption.TopDirectoryOnly);
        if (scripts.Length == 0) {
            return null;
        }

        var trees = scripts
            .Select(f => CSharpSyntaxTree.ParseText(
                File.ReadAllText(f.FullName),
                DefaultParseOptions,
                f.FullName));

        options ??= DefaultCompilationOptions;

        return CSharpCompilation.Create(
            $"cwl-script-{scriptDir.FullName.ShortPath().GetHashCode()}",
            trees,
            CurrentDomainReferences,
            options);
    }

    [Time]
    [Conditional("CWL_SCRIPTING")]
    internal static void CompileAllPackages()
    {
        var userPackages = BaseModManager.Instance.packages
            .Where(p => p is { builtin: false, activated: true, id: not null });

        foreach (var package in userPackages) {
            try {
                if (package.dirInfo.GetDirectories("Scripts") is not [{ } scripts]) {
                    continue;
                }

                var options = DefaultCompilationOptions
                    .WithModuleName(package.id.Replace(' ', '.').SanitizePath('-'));

                CwlMod.Log<CSharpCompilation>("cwl_log_csc_package".Loc(package.title, package.dirInfo.FullName.ShortPath()));

                // TODO: add hashed timestamp key

                var compilation = CompileScriptFromDir(scripts, options);
                if (compilation is null) {
                    continue;
                }
            } catch (Exception ex) {
                ExceptionProfile.GetFromStackTrace(ref ex).CreateAndPop();
                // noexcept
            }
        }
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
                if (assembly.IsDynamic || assembly.Location.IsEmptyOrNull) {
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

    private static HashSet<string> GetNamespaces(Assembly assembly)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in assembly.GetTypes()) {
            try {
                if (!type.IsPublic) {
                    continue;
                }

                var name = type.Namespace;
                if (!name.IsEmptyOrNull) {
                    namespaces.Add(name);
                }
            } catch {
                // noexcept
            }
        }

        return namespaces;
    }
}