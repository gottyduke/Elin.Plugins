using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EModding.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;
using Debug = UnityEngine.Debug;

namespace EModding;

[ConsoleCommandClassCustomizer("csc")]
public class EScriptCompiler
{
    private static readonly Dictionary<int, ScriptRunner<object>> _cachedScripts = [];

    public static string RoslynVersion =>
        field ??= typeof(Compilation).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

    [ConsoleCommand("clear_cache")]
    public static string ClearCache()
    {
        var count = _cachedScripts.Count;
        _cachedScripts.Clear();
        return "es_log_csc_cache_purge".lang(count.ToString());
    }

    // no need to trim references because it's never emitted
    internal static ScriptRunner<object> CompileScriptRunner(string script,
                                                             ScriptOptions options,
                                                             bool useCache = true,
                                                             bool throwOnError = false)
    {
        Debug.Log("es_log_csc_eval".lang(script));

        var scriptHash = script.GetHashCode();

        if (useCache && _cachedScripts.TryGetValue(scriptHash, out var cachedScript)) {
            return cachedScript;
        }

        var csharp = CSharpScript.Create(script, options, typeof(EScriptState));
        var compilation = csharp.GetCompilation();

        if (throwOnError) {
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Length > 0) {
                throw new EScriptCompilationException(errors);
            }
        }

        var runner = csharp.CreateDelegate();
        if (useCache) {
            _cachedScripts[scriptHash] = runner;
        }

        return runner;
    }

    internal static Compilation CompileScripts(IEnumerable<FileInfo> scripts,
                                               string assemblyName,
                                               CSharpCompilationOptions? options = null)
    {
        var trees = scripts
            .Select(s => CSharpSyntaxTree.ParseText(
                File.ReadAllText(s.FullName),
                EScriptOptions.DefaultParseOptions,
                s.FullName,
                Encoding.UTF8));

        options ??= EScriptOptions.DefaultCompilationOptions;

        return CSharpCompilation.Create(
            assemblyName,
            trees,
            EScriptOptions.CurrentDomainReferences,
            options);
    }

    internal static string GetPackageScriptName(string id)
    {
        return Regex.Replace(id, "[ ._]+", "-").SanitizeFileName('-');
    }

    public class PackageScriptCompiler(BaseModPackage package)
    {
        private static readonly string _roslynVersion = RoslynVersion;
        private readonly string _assemblyName = GetPackageScriptName(package.id);
        private readonly StringBuilder _sb = new();

        public string? Compile()
        {
            if (package.dirInfo.GetDirectories("Script") is not [{ } scriptDir] ||
                scriptDir.GetFiles("*.cs", SearchOption.AllDirectories) is not { Length: > 0 } scripts) {
                return null;
            }

            _sb.AppendLine($"Roslyn Version: {_roslynVersion}");
            _sb.AppendLine($"Assembly Name: {_assemblyName}");

            var assemblyPath = Path.Combine(package.dirInfo.FullName, $"{_assemblyName}.dll");

            // deterministic
            var scriptFiles = scripts
                .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _sb.AppendLine($"Script Count: {scriptFiles.Length}");
            foreach (var f in scriptFiles) {
                _sb.AppendLine($"- {Path.GetRelativePath(scriptDir.FullName, f.FullName)}");
            }

            var (hash, contents) = GetHashAndContents(scripts);
            _sb.AppendLine($"Script Hash: {hash}");

            // unload if already loaded
            EScriptLoader.TryUnloadScript(_assemblyName);

            if (File.Exists(assemblyPath)) {
                var existingHash = FileVersionInfo.GetVersionInfo(assemblyPath).ProductVersion;
                if (existingHash != hash) {
                    File.Delete(assemblyPath);
                    _sb.AppendLine($"Stale Hash: {existingHash}");
                } else {
                    return assemblyPath;
                }
            }

            var compilation = CompileScripts(contents, _assemblyName, EScriptOptions.DefaultCompilationOptions)
                .AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(
                        $"[assembly: System.Reflection.AssemblyInformationalVersion(\"{hash}\")]",
                        EScriptOptions.DefaultParseOptions,
                        encoding: Encoding.UTF8))
                .WithMinimalReferences();

            var references = compilation.References.ToArray();
            _sb.AppendLine($"References: {references.Length}");

            foreach (var reference in references) {
                _sb.AppendLine($"- {reference.Display.NormalizePath().ShortPath()}");
            }

            var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");

            var asmFs = File.Open(assemblyPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            var pdbFs = File.Open(pdbPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            var w32Ms = compilation.CreateDefaultWin32Resources(true, false, null, null);

            var emitResult = compilation.Emit(
                asmFs,
                pdbFs,
                win32Resources: w32Ms,
                options: new(debugInformationFormat: DebugInformationFormat.PortablePdb));

            asmFs.Dispose();
            pdbFs.Dispose();
            w32Ms.Dispose();

            if (!emitResult.Success) {
                throw new EScriptCompilationException(
                    emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            }

            _sb.AppendLine($"DLL size: {new FileInfo(assemblyPath).Length.ToAllocateString()}");
            _sb.AppendLine($"PDB size: {new FileInfo(pdbPath).Length.ToAllocateString()}");

            var log = _sb.ToString();
            Debug.Log(log);
            File.WriteAllText(Path.Combine(package.dirInfo.FullName, $"{_assemblyName}.log"), log, Encoding.UTF8);

            return assemblyPath;
        }

        public static (string sha, IEnumerable<FileInfo> contents) GetHashAndContents(IEnumerable<FileInfo> files)
        {
            List<FileInfo> fileContents = [];

            var sb = new StringBuilder();

            foreach (var file in files) {
                sb.Append($"{file.ShortPath()}_{file.LastWriteTimeUtc}");
                fileContents.Add(file);
            }

            return (sb.ToString().GetSha256Code(), fileContents);
        }
    }
}