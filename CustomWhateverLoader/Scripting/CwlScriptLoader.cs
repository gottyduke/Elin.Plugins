using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
    public enum CwlScriptAPIVersion
    {
        V1, // 1.21.0
    }

    public const CwlScriptAPIVersion APIVersion = CwlScriptAPIVersion.V1;

    public static string RoslynVersion =>
        field ??= typeof(Compilation).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

    [ConsoleCommand("unload")]
    public static string TryUnloadScript(string assemblyName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
        if (assembly is null) {
            return $"script {assemblyName} not found";
        }

        assembly.InvokeScriptMethod("CwlScriptUnload");

        return $"tried to unload {assemblyName}";
    }

    [ConsoleCommand("load")]
    public static string TryLoadScript(string assemblyPath)
    {
        if (!File.Exists(assemblyPath)) {
            return "script file not found";
        }

        var assembly = Assembly.LoadFrom(assemblyPath);

        assembly.InvokeScriptMethod("CwlScriptLoad");

        assembly.RegisterScript(assembly.GetName().Name);

        return "script loaded";
    }

    // no need to trim references because it's never emitted
    internal static (Compilation compilation, Script<object> csharp) CompileScript(string script,
                                                                                   ScriptOptions options,
                                                                                   bool throwOnError = false,
                                                                                   object? globals = null)
    {
        CwlMod.Log("cwl_log_csc_eval".Loc(script));

        var csharp = CSharpScript.Create(script, options, globals?.GetType());
        var compilation = csharp.GetCompilation();

        if (throwOnError) {
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Length > 0) {
                throw new ScriptCompilationException(errors);
            }
        }

        return (compilation, csharp);
    }

    internal static Compilation CompileScripts(IEnumerable<FileInfo> scripts,
                                               string assemblyName,
                                               CSharpCompilationOptions? options = null)
    {
        var trees = scripts
            .Select(s => CSharpSyntaxTree.ParseText(
                File.ReadAllText(s.FullName),
                DefaultParseOptions,
                s.FullName,
                Encoding.UTF8));

        options ??= DefaultCompilationOptions;

        return CSharpCompilation.Create(
            assemblyName,
            trees,
            CurrentDomainReferences,
            options);
    }

    [Time]
    [Conditional("CWL_SCRIPTING")]
    internal static void CompileAllPackages()
    {
        CwlMod.Log<CSharpCompilation>("cwl_log_csc_roslyn".Loc(RoslynVersion));

        var userPackages = BaseModManager.Instance.packages
            .Where(p => p is { builtin: false, activated: true, id: not null });

        foreach (var package in userPackages) {
            try {
                new CwlScriptCompiler(package).Compile();
            } catch (Exception ex) {
                CwlMod.ErrorWithPopup<CSharpCompilation>("cwl_error_csc_diag".Loc(package.title, ex.Message), ex);
                // noexcept
            }
        }
    }

    private static string GetPackageScriptName(string id)
    {
        return id.Replace(' ', '-').Replace('.', '-').SanitizeFileName('-');
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

    extension(Compilation compilation)
    {
        private Compilation WithMinimalReferences()
        {
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            // trimming is necessary so that generated assembly can be distributed
            var linkedSymbols = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
            foreach (var node in tree.GetRoot().DescendantNodes()) {
                var symbol = model.GetSymbolInfo(node).Symbol;
                if (symbol is IAssemblySymbol assemblySymbol) {
                    linkedSymbols.Add(assemblySymbol);
                }
            }

            HashSet<MetadataReference> trimmed = [];
            foreach (var metadata in compilation.References) {
                if (compilation.GetAssemblyOrModuleSymbol(metadata) is not IAssemblySymbol assembly) {
                    continue;
                }

                if (linkedSymbols.Contains(assembly) || _defaultReferences.Contains(assembly.Name)) {
                    trimmed.Add(metadata);
                }
            }

            return compilation.WithReferences(trimmed);
        }
    }

    extension(Assembly assembly)
    {
        private HashSet<string> GetNamespaces()
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

        public void RegisterScript(string mappedName)
        {
            TypeQualifier.MappedAssemblyNames[assembly] = mappedName;
            TypeQualifier.Declared.UnionWith(assembly.DefinedTypes);
        }

        public void UnregisterScript()
        {
            TypeQualifier.MappedAssemblyNames.Remove(assembly);
            TypeQualifier.Declared.ExceptWith(assembly.DefinedTypes);
        }

        public void RegisterDefaultScriptNamespaces()
        {
            var namespaces = assembly.GetNamespaces();
            if (namespaces.Count == 0) {
                return;
            }

            CurrentDomainNamespaces.UnionWith(namespaces);
        }

        public void InvokeScriptMethod(string methodName, params object[] args)
        {
            assembly.GetTypes()
                .SelectMany(t => t.GetMethods(AccessTools.all & ~BindingFlags.Static))
                .FirstOrDefault(mi => mi.Name == methodName)?
                .Invoke(null, args);
        }
    }
}