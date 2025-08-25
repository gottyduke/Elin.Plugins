using System;
using System.Linq;

namespace Cwl.Helper.String;

public static class ParamParser
{
    extension(string input)
    {
        public string?[] Parse(string delimiter, int expected = 0, bool useNull = true)
        {
            string?[]? parsed = input.Split(delimiter)
                .Select(s => s.Trim())
                .ToArray();

            Array.Resize(ref parsed, expected);
            for (var i = 0; i < parsed.Length; ++i) {
                parsed[i] ??= useNull ? null : "";
            }

            return parsed;
        }
    }
}