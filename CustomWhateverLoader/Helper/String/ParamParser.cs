using System;
using System.Linq;

namespace Cwl.Helper.String;

public static class ParamParser
{
    public static string?[] Parse(this string payload, string delimiter, int expected = 0, bool useNull = true)
    {
        string?[]? parsed = payload.Split(delimiter)
            .Select(s => s.Trim())
            .ToArray();

        Array.Resize(ref parsed, expected);
        for (var i = 0; i < parsed.Length; ++i) {
            parsed[i] ??= useNull ? null : "";
        }

        return parsed;
    }
}