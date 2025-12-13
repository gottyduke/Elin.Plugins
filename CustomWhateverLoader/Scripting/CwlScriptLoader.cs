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
using Microsoft.CodeAnalysis.Emit;
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
    internal static void CompileScriptPackage(BaseModPackage package)
    {
        if (package.dirInfo.GetDirectories("Scripts") is not [{ } scriptDir] ||
            scriptDir.GetFiles("*.cs", SearchOption.AllDirectories) is not { Length: > 0 } scripts) {
            return;
        }

        var log = new ScriptCompileLog();

        log.Log($"api version: {APIVersion}");
        log.Log($"roslyn version: {RoslynVersion}");
        log.Log($"package id: {package.id}");
        log.Log($"package dir: {package.dirInfo.FullName.ShortPath()}");

        var assemblyName = GetPackageScriptName(package.id);
        var assemblyPath = Path.Combine(package.dirInfo.FullName, $"{assemblyName}.dll");
        var logPath = Path.ChangeExtension(assemblyPath, "csc.log");

        log.Log($"assembly name: {assemblyName}");

        // deterministic
        var scriptFiles = scripts
            .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        log.Log($"script count: {scriptFiles.Length}");
        foreach (var f in scriptFiles) {
            log.Log($"  - {Path.GetRelativePath(scriptDir.FullName, f.FullName)}");
        }

        var (hash, contents) = GetHashAndContents();
        log.Log($"script hash: {hash}");

        // unload if already loaded
        TryUnloadScript(assemblyName);

        if (File.Exists(assemblyPath)) {
            var existingHash = FileVersionInfo.GetVersionInfo(assemblyPath).ProductVersion;
            log.Log($"existing assembly found with hash {existingHash}");

            if (existingHash != hash) {
                File.Delete(assemblyPath);
                log.Log($"old hash was '{existingHash}'");
            } else {
                return;
            }
        }

        var compilation = CompileScripts(contents, assemblyName, DefaultCompilationOptions)
            .AddSyntaxTrees(
                CSharpSyntaxTree.ParseText(
                    $"[assembly: System.Reflection.AssemblyInformationalVersion(\"{hash}\")]",
                    DefaultParseOptions,
                    encoding: Encoding.UTF8))
            .WithMinimalReferences();

        log.Log($"compilation trees: {compilation.SyntaxTrees.Count()}");

        var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");

        var asmFs = File.OpenWrite(assemblyPath);
        var pdbFs = File.OpenWrite(pdbPath);
        var w32Ms = compilation.CreateDefaultWin32Resources(true, false, null, null);

        var emitResult = compilation.Emit(
            asmFs,
            pdbFs,
            win32Resources: w32Ms,
            options: new(debugInformationFormat: DebugInformationFormat.PortablePdb));

        asmFs.Dispose();
        pdbFs.Dispose();
        w32Ms.Dispose();

        try {
            if (!emitResult.Success) {
                throw new ScriptCompilationException(emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            }

            log.Log($"DLL size: {new FileInfo(assemblyPath).Length.ToAllocateString()}");
            log.Log($"PDB size: {new FileInfo(pdbPath).Length.ToAllocateString()}");

            TryLoadScript(assemblyPath);

            log.WriteTo(logPath);
        } catch {
            if (File.Exists(assemblyPath)) {
                File.Delete(assemblyPath);
            }

            if (File.Exists(pdbPath)) {
                File.Delete(pdbPath);
            }

            throw;
        }

        return;

        (string sha, IEnumerable<FileInfo> contents) GetHashAndContents()
        {
            List<FileInfo> fileContents = [];

            using var sb = StringBuilderPool.Get();
            sb.Append(APIVersion.ToString());

            foreach (var file in scriptFiles) {
                sb.Append($"{file.ShortPath()}_{file.LastWriteTimeUtc}");
                fileContents.Add(file);
            }

            var hash64 = Convert.ToBase64String(sb.ToString().GetSha256Hash())
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return (hash64[..16], fileContents);
        }
    }


    [Conditional("CWL_SCRIPTING")]
    internal static void CompileAllPackages()
    {
        CwlMod.Log<CSharpCompilation>("cwl_log_csc_roslyn".Loc(RoslynVersion));

        var userPackages = BaseModManager.Instance.packages
            .Where(p => p is { builtin: false, activated: true, id: not null });

        foreach (var package in userPackages) {
            try {
                CompileScriptPackage(package);
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

    private class ScriptCompileLog
    {
        private readonly StringBuilderPool _sb = StringBuilderPool.Get();

        public void Log(string message)
        {
            _sb.AppendLine(message);
            CwlMod.Log<ScriptCompileLog>(message);
        }

        public void WriteTo(string path)
        {
            File.WriteAllText(path, _sb.ToString(), Encoding.UTF8);
        }

        ~ScriptCompileLog()
        {
            _sb.Dispose();
        }
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