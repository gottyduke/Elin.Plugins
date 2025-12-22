using System;
using System.Collections.Generic;
using Cwl.Helper.String;
using Cwl.LangMod;

namespace Emmersive.Helper;

public static class Localizer
{
    private static readonly HashSet<string> _unlocalized = new(StringComparer.Ordinal);

    internal static void DumpUnlocalized()
    {
        if (_unlocalized.Count == 0) {
            return;
        }

        using var sb = StringBuilderPool.Get();

        sb.AppendLine("unlocalized entries");

        foreach (var entry in _unlocalized) {
            sb.AppendLine(entry);
        }

        EmMod.Log(sb);
    }

    extension(string input)
    {
        public bool TryLocalize(out string result)
        {
            if (Lang.General.map.TryGetValue($"em_{input}", out var row)) {
                result = row.Loc();
                if (!result.IsEmptyOrNull) {
                    return true;
                }
            } else {
                _unlocalized.Add($"em_{input}");
            }

            result = input;
            return false;
        }
    }
}