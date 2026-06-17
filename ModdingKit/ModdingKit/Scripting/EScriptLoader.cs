using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using ReflexCLI;
using ReflexCLI.Attributes;
using Debug = UnityEngine.Debug;

namespace EModding;

[ConsoleCommandClassCustomizer("csc")]
public static class EScriptLoader
{
    [ConsoleCommand("unload")]
    public static string TryUnloadScript(string assemblyName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
        if (assembly is null) {
            return $"script {assemblyName} not found";
        }

        assembly.UnregisterScript();
        assembly.InvokeScriptMethod("EScriptUnload");

        ClassCache.assemblies.Remove(assembly.FullName);
        CommandRegistry.assemblies.Remove(assembly);

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
        assembly.InvokeScriptMethod("EScriptLoad");

        ClassCache.assemblies.Add(assembly.FullName);
        CommandRegistry.assemblies.Add(assembly);

        return "script loaded";
    }

    [ConsoleCommand("recompile")]
    internal static void LoadAllPackageScripts()
    {
        Debug.Log("es_log_csc_roslyn".lang(EScriptCompiler.RoslynVersion));

        foreach (var package in ModManager.Instance.ActivatedUserMods) {
            try {
                var loaded = new EScriptCompiler.PackageScriptCompiler(package).Compile();
                if (string.IsNullOrEmpty(loaded)) {
                    continue;
                }

                TryLoadScript(loaded);

                FileWatcherHelper.Register(
                    $"es_csc_{package.id}",
                    Path.Combine(package.dirInfo.FullName, "Script"),
                    "*.cs",
                    args => {
                        if ((args.ChangeType & WatcherChangeTypes.All) != 0) {
                            EGui.CreatePopupScoped(() => new("es_ui_csc_changed".lang(package.id)));
                        }
                    });
            } catch (Exception ex) {
                ModUtil.LogModError("es_error_csc_diag".lang(package.id, ex.Message), package as ModPackage);
                // noexcept
            }
        }
    }

    extension(Compilation compilation)
    {
        internal Compilation WithMinimalReferences()
        {
            HashSet<MetadataReference> trimmed = [];
            var selfAssembly = compilation.Assembly;
            var linkedSymbols = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

            foreach (var tree in compilation.SyntaxTrees) {
                var model = compilation.GetSemanticModel(tree);
                // trimming is necessary so that the generated assembly can be distributed
                foreach (var node in tree.GetRoot().DescendantNodes()) {
                    var info = model.GetSymbolInfo(node);
                    var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();

                    var assembly = symbol?.ContainingAssembly;
                    if (assembly is null) {
                        continue;
                    }

                    if (SymbolEqualityComparer.Default.Equals(assembly, selfAssembly)) {
                        continue;
                    }

                    linkedSymbols.Add(assembly);
                }

                foreach (var metadata in compilation.References) {
                    if (compilation.GetAssemblyOrModuleSymbol(metadata) is not IAssemblySymbol assembly) {
                        continue;
                    }

                    if (linkedSymbols.Contains(assembly) || EScriptOptions.DefaultReferences.Contains(assembly.Name)) {
                        trimmed.Add(metadata);
                    }
                }
            }

            return compilation.WithReferences(trimmed);
        }
    }

    extension(Assembly assembly)
    {
        public bool IsRoslynScript => assembly.GetName().Name.StartsWith("ℛ*");

        internal HashSet<string> GetNamespaces()
        {
            var namespaces = new HashSet<string>(StringComparer.Ordinal);

            foreach (var type in assembly.GetTypes()) {
                try {
                    if (!type.IsPublic) {
                        continue;
                    }

                    var name = type.Namespace;
                    if (!string.IsNullOrEmpty(name)) {
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
            BaseModManager.PublishEvent("escript.script_loaded", assembly);
        }

        public void UnregisterScript()
        {
            BaseModManager.PublishEvent("escript.script_unloaded", assembly);
        }

        public void HandoverScript() { }

        public void RegisterDefaultScriptNamespaces()
        {
            var namespaces = assembly.GetNamespaces();
            if (namespaces.Count == 0) {
                return;
            }

            EScriptOptions.CurrentDomainNamespaces.UnionWith(namespaces);
        }

        public void InvokeScriptMethod(string methodName, params object[] args)
        {
            var inits = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(AccessTools.all))
                .Where(mi => mi.Name == methodName);
            foreach (var init in inits) {
                try {
                    init.Invoke(null, args);
                } catch (Exception ex) {
                    Debug.LogError("es_error_csc_diag".lang(init.Name, ex.Message));
                }
            }
        }
    }
}