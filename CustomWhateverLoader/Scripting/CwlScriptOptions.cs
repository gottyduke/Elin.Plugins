using System;
using System.Collections.Generic;
using System.IO;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;

namespace Cwl.Scripting;

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
        "BepInEx",
        "BepInEx.Configuration",
        "HarmonyLib",
        // unity
        "UnityEngine",
        "UnityEngine.UI",
        // deps
        "ReflexCLI.Attributes",
        "Newtonsoft.Json",
        "Newtonsoft.Json.Serialization",
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

    private static readonly HashSet<string> _defaultReferences = [
        "0Harmony",
        "BepInEx.Core",
        "BepInEx.Unity",
        "Elin",
        "mscorlib",
        "Plugins.BaseCore",
        "Plugins.UI",
        "System.Core",
        "UnityEngine.CoreModule",
    ];

    internal static IReadOnlyList<MetadataReference> StaticDomainReferences => field ??= CreateStaticDomainReferences();

    internal static IReadOnlyList<MetadataReference> CurrentDomainReferences => [
        ..StaticDomainReferences,
        ..UserAssemblies.Values,
    ];

    internal static HashSet<string> CurrentDomainNamespaces => field ??= [.._defaultScriptNamespaces];
    internal static Dictionary<BaseModPackage, CompilationReference> UserAssemblies => field ??= [];

    // do not need debug info for scripts
    internal static CSharpParseOptions DefaultParseOptions =>
        field ??= new(
            LanguageVersion.Latest,
            DocumentationMode.Parse,
            SourceCodeKind.Regular,
            _defaultPreprocessors);

    // debug info for compilations
    internal static CSharpCompilationOptions DefaultCompilationOptions =>
        field ??= new(OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable,
            deterministic: true,
            optimizationLevel: OptimizationLevel.Release);

    internal static ScriptOptions DefaultScriptOptions =>
        field ??= ScriptOptions.Default
            .WithReferences(CurrentDomainReferences)
            .WithImports(CurrentDomainNamespaces)
            .WithOptimizationLevel(OptimizationLevel.Release);

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
}