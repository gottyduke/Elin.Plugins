using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.LangMod;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
    private static readonly Dictionary<int, ScriptRunner<object>> _cachedScripts = [];

    [ConsoleCommand("clear_cache")]
    public static string ClearCache()
    {
        var count = _cachedScripts.Count;
        _cachedScripts.Clear();
        return $"removed {count} cached scripts";
    }

    // no need to trim references because it's never emitted
    internal static ScriptRunner<object> CompileScriptRunner(string script,
                                                             ScriptOptions options,
                                                             bool useCache = true,
                                                             bool throwOnError = false)
    {
        CwlMod.Log("cwl_log_csc_eval".Loc(script));

        var scriptHash = script.GetHashCode();

        if (useCache && _cachedScripts.TryGetValue(scriptHash, out var cachedScript)) {
            return cachedScript;
        }

        var csharp = CSharpScript.Create(script, options, typeof(CwlScriptState));
        var compilation = csharp.GetCompilation();

        if (throwOnError) {
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Length > 0) {
                throw new ScriptCompilationException(errors);
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

    internal static string GetPackageScriptName(string id)
    {
        return Regex.Replace(id, "[ ._]+", "-").SanitizeFileName('-');
    }

    public class PackageScriptCompiler(BaseModPackage package)
    {
        private static readonly string _apiVersion = APIVersion.ToString();
        private static readonly string _cwlVersion = ModInfo.BuildVersion;
        private static readonly string _roslynVersion = RoslynVersion;
        private readonly string _assemblyName = GetPackageScriptName(package.id);
        private readonly StringBuilderPool _sb = StringBuilderPool.Get();

        public string? Compile()
        {
            if (package.dirInfo.GetDirectories("Script") is not [{ } scriptDir] ||
                scriptDir.GetFiles("*.cs", SearchOption.AllDirectories) is not { Length: > 0 } scripts) {
                return null;
            }

            _sb.AppendLine($"CWL Version: {_cwlVersion}");
            _sb.AppendLine($"API Version: {_apiVersion}");
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
            TryUnloadScript(_assemblyName);

            if (File.Exists(assemblyPath)) {
                var existingHash = FileVersionInfo.GetVersionInfo(assemblyPath).ProductVersion;
                if (existingHash != hash) {
                    File.Delete(assemblyPath);
                    _sb.AppendLine($"Stale Hash: {existingHash}");
                } else {
                    return assemblyPath;
                }
            }

            var compilation = CompileScripts(contents, _assemblyName, DefaultCompilationOptions)
                .AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(
                        $"[assembly: System.Reflection.AssemblyInformationalVersion(\"{hash}\")]",
                        DefaultParseOptions,
                        encoding: Encoding.UTF8))
                .WithMinimalReferences();

            var references = compilation.References.ToArray();
            _sb.AppendLine($"References: {references.Length}");

            foreach (var reference in references) {
                _sb.AppendLine($"- {reference.Display.NormalizePath().ShortPath()}");
            }

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
                    throw new ScriptCompilationException(
                        emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
                }

                _sb.AppendLine($"DLL size: {new FileInfo(assemblyPath).Length.ToAllocateString()}");
                _sb.AppendLine($"PDB size: {new FileInfo(pdbPath).Length.ToAllocateString()}");
            } catch {
                if (File.Exists(assemblyPath)) {
                    File.Delete(assemblyPath);
                }

                if (File.Exists(pdbPath)) {
                    File.Delete(pdbPath);
                }

                throw;
            }

            var log = _sb.ToString();
            CwlMod.Log<PackageScriptCompiler>(log);
            File.WriteAllText(Path.Combine(package.dirInfo.FullName, $"{_assemblyName}.log"), log, Encoding.UTF8);

            return assemblyPath;
        }

        public static (string sha, IEnumerable<FileInfo> contents) GetHashAndContents(IEnumerable<FileInfo> files)
        {
            List<FileInfo> fileContents = [];

            using var sb = StringBuilderPool.Get();
            sb.Append(APIVersion.ToString());

            foreach (var file in files) {
                sb.Append($"{file.ShortPath()}_{file.LastWriteTimeUtc}");
                fileContents.Add(file);
            }

            return (sb.ToString().GetSha256Code(), fileContents);
        }

        ~PackageScriptCompiler()
        {
            _sb.Dispose();
        }
    }
}