using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        assembly.UnregisterScript();
        assembly.InvokeScriptMethod("CwlScriptUnload");

        return $"tried to unload {assemblyName}";
    }

    [ConsoleCommand("load")]
    public static string TryLoadScript(string assemblyPath)
    {
        if (!File.Exists(assemblyPath)) {
            return "script file not found";
        }

        var assembly = Assembly.Load(File.ReadAllBytes(assemblyPath));

        assembly.RegisterScript(assembly.GetName().Name);
        assembly.InvokeScriptMethod("CwlScriptLoad");

        return "script loaded";
    }

    [Time]
    [Conditional("CWL_SCRIPTING")]
    [ConsoleCommand("recompile")]
    internal static void LoadAllPackageScripts()
    {
        CwlMod.Log<CSharpCompilation>("cwl_log_csc_roslyn".Loc(RoslynVersion));

        var userPackages = BaseModManager.Instance.packages
            .Where(p => p is { builtin: false, activated: true, id: not null });

        foreach (var package in userPackages) {
            try {
                var loaded = new PackageScriptCompiler(package).Compile();
                if (loaded.IsEmptyOrNull) {
                    continue;
                }

                TryLoadScript(loaded);

                FileWatcherHelper.Register(
                    $"cwl_csc_{package.id}",
                    Path.Combine(package.dirInfo.FullName, "Script"),
                    "*.cs",
                    args => {
                        if ((args.ChangeType & WatcherChangeTypes.All) != 0) {
                            CwlMod.Popup<PackageScriptCompiler>("cwl_ui_csc_changed".Loc(package.id));
                        }
                    });
            } catch (Exception ex) {
                CwlMod.ErrorWithPopup<CSharpCompilation>("cwl_error_csc_diag".Loc(package.title, ex.Message), ex);
                // noexcept
            }
        }
    }

    extension(Compilation compilation)
    {
        private Compilation WithMinimalReferences()
        {
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            // trimming is necessary so that the generated assembly can be distributed
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
        public bool IsRoslynScript => assembly.GetName().Name.StartsWith("ℛ*");

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

        public void HandoverScript() { }

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
                .SelectMany(t => t.GetMethods(AccessTools.all))
                .FirstOrDefault(mi => mi.Name == methodName)?
                .Invoke(null, args);
        }
    }
}