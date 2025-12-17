using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cwl.API.Processors;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

[ConsoleCommandClassCustomizer("cwl.csc")]
public class CwlScriptSubmission(string submissionKey)
{
    private const string CacheStorage = $"{ModInfo.Name}/ScriptSubmissions";

    private static readonly Dictionary<string, CwlScriptSubmission> _submissions = new(StringComparer.Ordinal);
    private static readonly GameIOProcessor.GameIOContext _context = GameIOProcessor.GetPersistentModContext(CacheStorage)!;
    private static readonly Dictionary<int, Func<object?, object?>> _cachedScripts = [];

    private readonly string _snippets = _context.GetPath(submissionKey);

    [ConsoleCommand("create_submission")]
    public static CwlScriptSubmission Create(string submissionKey)
    {
        if (_submissions.TryGetValue(submissionKey, out var submission)) {
            return submission;
        }

        submission = _submissions[submissionKey] = new(submissionKey);
        Directory.CreateDirectory(submission._snippets);

        return submission;
    }

    [ConsoleCommand("clear_submissions")]
    public static void InvalidateSubmission(string submissionKey = "")
    {
        if (submissionKey.IsEmptyOrNull) {
            _context.Clear();
        } else {
            try {
                Directory.Delete(_context.GetPath(submissionKey));
            } catch {
                // noexcept
            }
        }

        _submissions.Clear();
    }

    public Func<object?, object?>? CompileAndRun<T>(string script)
    {
        var scriptKey = $"{typeof(T).Name}_{script}".GetHashCode();
        if (_cachedScripts.TryGetValue(scriptKey, out var scriptRunner)) {
            return scriptRunner;
        }

        var scriptCache = Path.Combine(_snippets, scriptKey.ToString());

        Assembly? assembly = null;

        // TODO: loc
        if (File.Exists(scriptCache)) {
            try {
                assembly = Assembly.Load(File.ReadAllBytes(scriptCache));
                CwlMod.Log<CwlScriptSubmission>($"loading from script cache data-{scriptKey}");
            } catch {
                File.Delete(scriptCache);
                // noexcept
            }
        }

        if (assembly is null) {
            var csharp = CSharpScript.Create(script, CwlScriptLoader.DefaultScriptOptions, typeof(T));
            var compilation = csharp.GetCompilation();

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Length > 0) {
                throw new ScriptCompilationException(errors);
            }

            using var fs = File.Open(scriptCache, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            compilation.Emit(fs);

            CwlMod.Log<CwlScriptSubmission>($"created script cache data-{scriptKey}");

            using var ms = new MemoryStream();
            fs.Seek(0, SeekOrigin.Begin);
            fs.CopyTo(ms);

            assembly = Assembly.Load(ms.ToArray());
        }

        var submission = CreateSubmissionCall(assembly);
        if (submission is null) {
            CwlMod.Log<CwlScriptSubmission>($"failed to script submission data-{scriptKey}");
            return null;
        }

        return _cachedScripts[scriptKey] = submission;
    }

    /// <summary>
    ///     Follows Roslyn API
    /// </summary>
    public static Func<object?, object?>? CreateSubmissionCall(Assembly assembly)
    {
        var factory = assembly.GetType("Submission#0")?
            .GetMethod("<Factory>")?
            .CreateDelegate(typeof(Func<object?[], Task<object>>));
        if (factory is not Func<object?[], Task<object>> submission) {
            return null;
        }

        return InvokeSubmission;

        object? InvokeSubmission(object? globals = null) =>
            // submission[globals, this]
            submission([globals, null])
                .GetAwaiter()
                .GetResult();
    }
}