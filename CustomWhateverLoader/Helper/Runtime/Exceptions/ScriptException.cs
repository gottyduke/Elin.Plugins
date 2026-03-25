using System;
using System.Collections.Generic;
using System.IO;
using Cwl.Helper.String;
using Cwl.LangMod;
using Microsoft.CodeAnalysis;

namespace Cwl.Helper.Exceptions;

public class ScriptException(string message) : Exception(message);

public class ScriptLoaderNotReadyException()
    : ScriptException("cwl_error_cs_disabled".lang());

public class ScriptCompilationException(IEnumerable<Diagnostic> diagnostics)
    : ScriptException(SetSourceSpan(diagnostics))
{
    private static string SetSourceSpan(IEnumerable<Diagnostic> diagnostics)
    {
        using var sb = StringBuilderPool.Get();
        foreach (var diagnostic in diagnostics) {
            var location = diagnostic.Location;
            if (location.IsInSource) {
                var lineSpan = location.GetLineSpan();
                var lineNumber = lineSpan.StartLinePosition.Line;
                var file = Path.GetFileNameWithoutExtension(location.SourceTree.FilePath);
                sb.Append($"#[{file}@{lineNumber + 1}] ");
            }
            sb.AppendLine(diagnostic.GetMessage());
        }
        return sb.ToString();
    }
}

public class ScriptDisabledException()
    : ScriptException("cwl_error_cs_disabled".lang());

public class ScriptStateFrozenException(string frozen)
    : ScriptException("cwl_error_cs_frozen".Loc(frozen));