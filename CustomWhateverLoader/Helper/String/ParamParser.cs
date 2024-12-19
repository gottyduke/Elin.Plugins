using System;
using System.Linq;

namespace Cwl.Helper.String;

public static class ParamParser
{
    public static string[] Parse(this string payload, string delimiter, int expected = 0)
    {
        var parsed = payload.Split(delimiter)
            .Select(s => s.Trim())
            .ToArray();
        if (parsed.Length >= expected) {
            return parsed;
        }

        Array.Resize(ref parsed, expected);
        return parsed;
    }
}