using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace EModding.Helper.Runtime.Exceptions;

public class EScriptCompilationException(IEnumerable<Diagnostic> diagnostics) : EScriptException(SetSourceSpan(diagnostics))
{
    private static string SetSourceSpan(IEnumerable<Diagnostic> diagnostics)
    {
        var sb = new StringBuilder();
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