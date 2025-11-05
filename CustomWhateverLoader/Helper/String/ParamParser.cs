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

        public string ExtractInBetween(string left, string right, string fallback = "")
        {
            if (input.Length <= 2) {
                return fallback;
            }

            var leftIndex = input.IndexOf(left, StringComparison.InvariantCultureIgnoreCase);
            if (leftIndex == -1) {
                return fallback;
            }

            var contentIndex = leftIndex + left.Length;

            var rightIndex = input.IndexOf(right, contentIndex, StringComparison.InvariantCultureIgnoreCase);
            return rightIndex == -1
                ? fallback
                : input[contentIndex..rightIndex];
        }

        public string ExtractInBetween(char left, char right, string fallback = "")
        {
            var leftIndex = input.IndexOf(left);
            if (leftIndex == -1) {
                return fallback;
            }

            var contentIndex = leftIndex + 1;
            var rightIndex = input.IndexOf(right, contentIndex);
            return rightIndex == -1
                ? fallback
                : input[contentIndex..rightIndex];
        }
    }
}