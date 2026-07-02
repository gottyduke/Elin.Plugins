using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using EModding.Helper.Runtime.Exceptions;

namespace Exm.LangMod;

public static class LocFormatter
{
    private static readonly HashSet<string> _unlocalized = new(StringComparer.Ordinal);

    [Conditional("DEBUG")]
    internal static void DumpUnlocalized()
    {
        if (_unlocalized.Count == 0) {
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("unlocalized entries");

        foreach (var entry in _unlocalized) {
            sb.AppendLine(entry);
        }

        ExmMod.Log(sb);
    }

    extension(string id)
    {
        public string Loc(params object?[] args)
        {
            var lang = id.lang();
            if (lang == id) {
                _unlocalized.Add(id);
            }

            if (args.Length == 0) {
                return lang;
            }

            try {
                return string.Format(lang, args);
            } catch (Exception ex) {
                var fmt = string.Join(", ", [id, ..args]);
                ExmMod.Warn($"log fmt failure / {fmt}");
                return DebugThrow.Return(ex, fmt);
            }
        }
    }

    extension(LangGeneral.Row row)
    {
        public string Loc(params object?[] args)
        {
            var lang = Lang.isBuiltin
                ? string.IsNullOrEmpty(row.text_L) && !string.IsNullOrEmpty(row.text) ? row.text : row.text_L
                : Lang.isJP
                    ? row.text_JP
                    : row.text;
            if (args.Length == 0) {
                return lang;
            }

            try {
                return string.Format(lang, args);
            } catch (Exception ex) {
                var fmt = string.Join(", ", [row.id, ..args]);
                ExmMod.Warn($"log fmt failure / {fmt}");
                return DebugThrow.Return(ex, fmt);
            }
        }
    }
}