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
using MethodTimer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
    private static readonly Dictionary<int, Script<object>> _cachedScripts = [];

    [ConsoleCommand("clear_cache")]
    public static string ClearCache()
    {
        var count = _cachedScripts.Count;
        _cachedScripts.Clear();
        return $"removed {count} cached scripts";
    }

    // no need to trim references because it's never emitted
    internal static Script<object> CompileScript(string script,
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

        if (useCache) {
            _cachedScripts[scriptHash] = csharp;
        }

        return csharp;
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

    public class CwlScriptCompiler(BaseModPackage package)
    {
        private readonly string _apiVersion = APIVersion.ToString();
        private readonly string _assemblyName = GetPackageScriptName(package.id);
        private readonly string _cwlVersion = ModInfo.BuildVersion;
        private readonly string _roslynVersion = RoslynVersion;
        private readonly StringBuilderPool _sb = StringBuilderPool.Get();

        public void Compile()
        {
            if (package.dirInfo.GetDirectories("Scripts") is not [{ } scriptDir] ||
                scriptDir.GetFiles("*.cs", SearchOption.AllDirectories) is not { Length: > 0 } scripts) {
                return;
            }

            _sb.AppendLine($"CWL version: {_cwlVersion}");
            _sb.AppendLine($"API Version: {_apiVersion}");
            _sb.AppendLine($"roslyn Version: {_roslynVersion}");
            _sb.AppendLine($"assembly name: {_assemblyName}");

            var assemblyPath = Path.Combine(package.dirInfo.FullName, $"{_assemblyName}.dll");

            // deterministic
            var scriptFiles = scripts
                .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _sb.AppendLine($"script count: {scriptFiles.Length}");
            foreach (var f in scriptFiles) {
                _sb.AppendLine($"- {Path.GetRelativePath(scriptDir.FullName, f.FullName)}");
            }

            var (hash, contents) = GetHashAndContents(scripts);
            _sb.AppendLine($"script hash: {hash}");

            // unload if already loaded
            TryUnloadScript(_assemblyName);

            if (File.Exists(assemblyPath)) {
                var existingHash = FileVersionInfo.GetVersionInfo(assemblyPath).ProductVersion;
                _sb.AppendLine($"existing assembly found with hash '{existingHash}'");

                if (existingHash != hash) {
                    File.Delete(assemblyPath);
                    _sb.AppendLine($"stale hash: '{existingHash}'");
                } else {
                    return;
                }
            }

            var compilation = CompileScripts(contents, _assemblyName, DefaultCompilationOptions)
                .AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(
                        $"[assembly: System.Reflection.AssemblyInformationalVersion(\"{hash}\")]",
                        DefaultParseOptions,
                        encoding: Encoding.UTF8))
                .WithMinimalReferences();

            _sb.AppendLine($"compilation trees: {compilation.SyntaxTrees.Count()}");

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

                TryLoadScript(assemblyPath);
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
            CwlMod.Log<CwlScriptCompiler>(_sb.ToString());
            File.WriteAllText(Path.Combine(package.dirInfo.FullName, $"{_assemblyName}.log"), log, Encoding.UTF8);
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

        private static string GetPackageScriptName(string id)
        {
            return Regex.Replace(id, "[ ._]+", "-").SanitizeFileName('-');
        }

        ~CwlScriptCompiler()
        {
            _sb.Dispose();
        }
    }
}