using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Emmersive.Helper;
using EModding.Helper.Runtime.Exceptions;
using Debug = UnityEngine.Debug;

namespace Emmersive.LangMod;

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

        Debug.Log(sb);
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
                EmMod.Warn($"log fmt failure / {fmt}");
                return DebugThrow.Return(ex, fmt);
            }
        }
    }

    extension(LangGeneral.Row row)
    {
        public string Loc(params object?[] args)
        {
            var lang = Lang.isBuiltin
                ? row.text_L.IsEmptyOrNull && !row.text.IsEmptyOrNull ? row.text : row.text_L
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
                EmMod.Warn($"log fmt failure / {fmt}");
                return DebugThrow.Return(ex, fmt);
            }
        }
    }
}