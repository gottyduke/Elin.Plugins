using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;

namespace EModding;

internal class EScriptOptions
{
    internal static readonly string[] DefaultScriptNamespaces = [
        "System",
        "System.Collections",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Reflection",
        "HarmonyLib",
        // modding kit
        "EModding",
        "EModding.API",
        "EModding.Helper",
        "EModding.Helper.Runtime",
        "EModding.Helper.Runtime.Exceptions",
        // unity
        "UnityEngine",
        "UnityEngine.UI",
        // deps
        "ReflexCLI.Attributes",
        "Newtonsoft.Json",
        "Newtonsoft.Json.Serialization",
    ];

    internal static readonly string[] DefaultPreprocessors = [
#if DEBUG
        "DEBUG",
#endif
        "TRACE",
#if NIGHTLY
        "NIGHTLY",
#endif
    ];

    internal static readonly HashSet<string> DefaultReferences = [
        "0Harmony",
        "BepInEx.Core",
        "BepInEx.Unity",
        "Elin",
        "mscorlib",
        "Plugins.ActorSystem",
        "Plugins.BaseCore",
        "Plugins.Modding",
        "Plugins.Scripting",
        "Plugins.Sound",
        "Plugins.UI",
        "Reflex",
        "System.Core",
        "UnityEngine",
        "UnityEngine.CoreModule",
        "ElinModdingKit",
    ];

    internal static IReadOnlyList<MetadataReference> StaticDomainReferences => field ??= CreateStaticDomainReferences();

    internal static IReadOnlyList<MetadataReference> CurrentDomainReferences => [
        ..StaticDomainReferences,
        ..UserAssemblies.Values,
    ];

    internal static HashSet<string> CurrentDomainNamespaces => field ??= [..DefaultScriptNamespaces];
    internal static Dictionary<BaseModPackage, CompilationReference> UserAssemblies => field ??= [];

    // do not need debug info for scripts
    internal static CSharpParseOptions DefaultParseOptions =>
        field ??= new(
            LanguageVersion.Latest,
            DocumentationMode.Parse,
            SourceCodeKind.Regular,
            DefaultPreprocessors);

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
                if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)) {
                    continue;
                }

                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            } catch {
                // noexcept
            }
        }

        return references;
    }
}