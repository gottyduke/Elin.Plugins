using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
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
        }

        public (string sha, IEnumerable<FileInfo> contents) GetHashAndContents(IEnumerable<FileInfo> files)
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

        ~CwlScriptCompiler()
        {
            var log = _sb.ToString();

            CwlMod.Log<CwlScriptCompiler>(_sb.ToString());
            File.WriteAllText(Path.Combine(package.dirInfo.FullName, $"{_assemblyName}.log"), log, Encoding.UTF8);

            _sb.Dispose();
        }
    }
}