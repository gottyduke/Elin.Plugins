using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Cwl.API.Drama;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Dramas;

[HarmonyPatch]
internal class LoadDramaPatch
{
    private const string CacheEntry = "Dialog/Drama/";
    private const string Pattern = "*.xlsx";

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.Load))]
    internal static IEnumerable<CodeInstruction> OnLoadIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Callvirt, nameof(ExcelData.BuildList)))
            .EnsureValid("drama build list")
            .Repeat(match => match.InsertAndAdvance(
                    new(OpCodes.Pop),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldarg_0),
                    Transpilers.EmitDelegate(BuildRelocatedList))
                .RemoveInstruction())
            .InstructionEnumeration();
    }

    [Time]
    private static List<Dictionary<string, string>> BuildRelocatedList(ExcelData data,
                                                                       string oldPath,
                                                                       DramaManager dm)
    {
        var book = dm.setup.book;
        var sheet = dm.setup.sheet;
        var lang = Lang.langCode;

        var cachedBookName = $"{CacheEntry}{book}_{lang}";
        if (PackageIterator.TryLoadFromPackageCache(cachedBookName, out var cachedPath)) {
            data.path = cachedPath;
            // force a list text sync
            return SyncTexts(data.BuildList(sheet));
        }

        var books = PackageIterator.GetLangFilesFromPackage(Pattern)
            .Where(b => b.Contains(CacheEntry))
            .Where(s => Path.GetFileNameWithoutExtension(s) == book)
            .OrderBy(b => b)
            .ToArray();

        // Elona Dialog/Drama files are not in their LangCode subdirectory
        var fallback = books.FirstOrDefault();
        // 1.19.5 change to last to allow mapping vanilla dramas
        var localized = books.LastOrDefault(b => b.Contains($"/{lang}/")) ?? fallback;

        if (localized is null) {
            throw new FileNotFoundException(book);
        }

        if (data.path.NormalizePath() != localized) {
            CwlMod.Log<DramaManager>("cwl_relocate_drama".Loc(cachedBookName, Pattern, localized.ShortPath()));
        }

        PackageIterator.AddCachedPath(cachedBookName, localized);
        data.path = localized;

        // force a list text sync
        return SyncTexts(data.BuildList(sheet));
    }

    // make drama writer life easier
    private static List<Dictionary<string, string>> SyncTexts(List<Dictionary<string, string>> list)
    {
        var langKey = $"text_{Lang.langCode}";

        foreach (var item in list) {
            item.TryAdd("text", "");
            item.TryAdd("text_EN", "");
            item.TryAdd("text_JP", "");

            var textEn = item["text_EN"];
            var textJp = item["text_JP"];
            var textLocalize = item["text"];

            if (!item.TryGetValue(langKey, out var textLang)) {
                textLang = textLocalize.OrIfEmpty(textEn.OrIfEmpty(textJp));
            }

            item["text"] = textLang;

            if (textEn.IsEmptyOrNull) {
                item["text_EN"] = textLang;
            }

            if (textJp.IsEmptyOrNull) {
                item["text_JP"] = textLang;
            }
        }

        return list;
    }

    // prevent duplicate loc id if drama writer is careless
    private static List<Dictionary<string, string>> SanitizeId(List<Dictionary<string, string>> lists)
    {
        if (lists.Count == 0) {
            return lists;
        }

        try {
            HashSet<string> allIds = new(lists.Select(kv => kv["id"]), StringComparer.InvariantCultureIgnoreCase);

            var nextId = allIds.Count;
            for (var i = lists.Count - 1; i >= 0; --i) {
                var dict = lists[i];
                var id = dict["id"];

                if (id.IsEmptyOrNull) {
                    if (!dict["text"].IsEmptyOrNull) {
                        dict["id"] = GetNewId();
                    }

                    continue;
                }

                if (allIds.Add(id)) {
                    continue;
                }

                dict["id"] = GetNewId();
            }

            return lists;

            string GetNewId()
            {
                string newId;
                do {
                    newId = $"cwl_dm_id_{nextId++}";
                } while (!allIds.Add(newId));

                return newId;
            }
        } catch (Exception ex) {
            CwlMod.Warn<DramaExpansion>("cwl_error_failure".Loc(ex.Message));
            return lists;
            // noexcept
        }
    }
}