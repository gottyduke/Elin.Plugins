using System;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;

namespace Cwl.LangMod;

public static class LocFormatter
{
    extension(string id)
    {
        public string Loc(params object?[] args)
        {
            var lang = id.lang();
            if (args.Length == 0) {
                return lang;
            }

            try {
                return string.Format(lang, args);
            } catch (Exception ex) {
                var fmt = string.Join(", ", [id, ..args]);
                CwlMod.Warn($"log fmt failure / {fmt}");
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
                CwlMod.Warn($"log fmt failure / {fmt}");
                return DebugThrow.Return(ex, fmt);
            }
        }
    }
}