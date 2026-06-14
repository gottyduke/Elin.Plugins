using System.IO;
using ElinTogether.Helper.String;
using Serilog.Formatting.Json;

namespace ElinTogether;

internal class PlainJsonValueFormatter() : JsonValueFormatter(null)
{
    protected override void FormatLiteralValue(object? value, TextWriter output)
    {
        if (value is string s) {
            s = s.RemoveTagColor();
            base.FormatLiteralValue(s, output);
        } else {
            base.FormatLiteralValue(value, output);
        }
    }
}