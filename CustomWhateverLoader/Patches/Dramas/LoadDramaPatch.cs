using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
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
        var cm = new CodeMatcher(instructions);
        return cm
            .MatchEndForward(
                new OperandContains(OpCodes.Callvirt, nameof(ExcelData.BuildList)))
            .ThrowIfInvalid("failed to match drama build list")
            .Repeat(match => match
                .InsertAndAdvance(
                    new(OpCodes.Pop),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldarg_0),
                    Transpilers.EmitDelegate(BuildRelocatedList))
                .RemoveInstruction())
            .Start()
            .MatchStartForward(
                new OpCodeContains(nameof(OpCodes.Ldloc)),
                new OperandContains(OpCodes.Ldstr, "id"),
                new OperandContains(OpCodes.Callvirt, "Item"))
            .ThrowIfInvalid("failed to match id insert")
            .InsertAndAdvance(
                cm.Instruction,
                Transpilers.EmitDelegate(SyncTexts))
            .InstructionEnumeration();
    }

    [Time]
    private static List<Dictionary<string, string>> BuildRelocatedList(ExcelData data, string oldPath,
        DramaManager dm)
    {
        var book = dm.setup.book;
        var sheet = dm.setup.sheet;
        var lang = Lang.langCode;

        try {
            var cachedBookName = $"{CacheEntry}{book}_{lang}";
            if (PackageIterator.TryLoadFromPackageCache(cachedBookName, out var cachedPath)) {
                data.path = cachedPath;
                return data.BuildList(sheet);
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
        } catch (Exception ex) {
            ELayerCleanup.Cleanup<LayerDrama>();

            var exp = ExceptionProfile.GetFromStackTrace(ex);
            exp.StartAnalyzing();
            exp.CreateAndPop("cwl_warn_drama_play_ex".Loc(ex.Message));
            // noexcept
        }

        return data.BuildList(sheet);
    }

    // make drama writer life easier
    private static void SyncTexts(Dictionary<string, string> item)
    {
        var id = item["id"];
        if (id.IsEmpty()) {
            return;
        }

        item.TryAdd("text", "");
        item.TryAdd("text_EN", "");
        item.TryAdd("text_JP", "");

        if (item.TryGetValue($"text_{Lang.langCode}", out var textLang)) {
            item["text"] = textLang;
        }

        var textLocalize = item["text"];
        var textEn = item["text_EN"];
        var textJp = item["text_JP"];

        if (textEn.IsEmpty()) {
            item["text_EN"] = textLocalize.IsEmpty(textJp.IsEmpty("<empty>"));
        }

        if (textJp.IsEmpty()) {
            item["text_JP"] = textLocalize.IsEmpty(textEn.IsEmpty("<empty>"));
        }
    }

    private static List<Dictionary<string, string>> SanitizeId(List<Dictionary<string, string>> lists)
    {
        if (lists.Count == 0) {
            return lists;
        }

        HashSet<string> allIds = new([..lists.Select(kv => kv["id"])], StringComparer.Ordinal);

        var nextId = allIds.Count;
        for (var i = lists.Count - 1; i >= 0; --i) {
            var dict = lists[i];
            var id = dict["id"];

            if (id.IsEmpty()) {
                if (!dict["text"].IsEmpty()) {
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
    }
}