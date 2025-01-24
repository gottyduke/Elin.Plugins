using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Relocation;

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
            .Repeat(cm => cm
                .InsertAndAdvance(
                    new(OpCodes.Pop),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldarg_0),
                    Transpilers.EmitDelegate(BuildRelocatedList))
                .RemoveInstruction())
            .InstructionEnumeration();
    }

    [Time]
    private static List<Dictionary<string, string>> BuildRelocatedList(ExcelData data, string oldPath,
        DramaManager dm)
    {
        var book = dm.setup.book;
        var sheet = dm.setup.sheet;
        var lang = Lang.langCode;

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
        var fallback = books.First();
        // 1.19.5 change to last to allow mapping vanilla dramas
        var localized = books.LastOrDefault(b => b.Contains($"/{lang}/")) ?? fallback;

        if (data.path.NormalizePath() != localized) {
            CwlMod.Log<DramaManager>("cwl_relocate_drama".Loc(cachedBookName, Pattern, localized.ShortPath()));
        }

        PackageIterator.AddCachedPath(cachedBookName, localized);
        data.path = localized;
        return data.BuildList(sheet);
    }
}