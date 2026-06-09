using System;
using System.IO;
using System.Linq;
using System.Text;
using EModding.Helper.Runtime.Exceptions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using DiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;

namespace EModding;

internal class EScriptProvider : IScriptProvider
{
    public string GetApiVersion()
    {
        return "es_log_csc_roslyn".lang(EScriptCompiler.RoslynVersion);
    }

    public object? EvaluateScript(string script, object? globals = null, bool throwOnError = false)
    {
        var runner = CompileScript(script, globals?.GetType(), throwOnError);
        return runner(globals);
    }

    public EScriptRunner CompileScript(string script, Type? globalsType = null, bool throwOnError = false)
    {
        var csharp = CSharpScript.Create(script, EScriptOptions.DefaultScriptOptions, globalsType);
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
        return g => runner(g)
            .GetAwaiter()
            .GetResult();
    }

    public byte[] CompileScriptAssembly(string script, Type? globalsType = null, bool throwOnError = false)
    {
        var csharp = CSharpScript.Create(script, EScriptOptions.DefaultScriptOptions, globalsType);
        var compilation = csharp.GetCompilation();

        if (throwOnError) {
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Length > 0) {
                throw new EScriptCompilationException(errors);
            }
        }

        using var ms = new MemoryStream();
        compilation.Emit(ms);
        return ms.ToArray();
    }

    public byte[] CompileAssembly((string content, string filePath)[] codes, string assemblyName, bool throwOnError = false)
    {
        var ast = codes
            .Select(c => CSharpSyntaxTree.ParseText(
                c.content,
                EScriptOptions.DefaultParseOptions,
                c.filePath,
                Encoding.UTF8));
        var compilation = CSharpCompilation.Create(
            assemblyName,
            ast,
            EScriptOptions.CurrentDomainReferences,
            EScriptOptions.DefaultCompilationOptions);

        if (throwOnError) {
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Length > 0) {
                throw new EScriptCompilationException(errors);
            }
        }

        using var ms = new MemoryStream();
        compilation.Emit(ms);
        return ms.ToArray();
    }

    public bool IsAvailable => true;
}