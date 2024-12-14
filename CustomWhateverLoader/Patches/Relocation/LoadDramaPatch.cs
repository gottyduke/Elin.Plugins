using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Cwl.Helper.File;
using Cwl.Helper.String;
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
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(ExcelData),
                    nameof(ExcelData.BuildList),
                    [typeof(string)])))
            .Repeat(cm => cm
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
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
        if (PackageFileIterator.TryLoadFromPackage(cachedBookName, out var cachedPath)) {
            data.path = cachedPath;
            return data.BuildList(sheet);
        }

        var books = PackageFileIterator.GetLangFilesFromPackage(Pattern)
            .Where(b => b.Contains(CacheEntry))
            .Where(s => Path.GetFileNameWithoutExtension(s) == book)
            .OrderBy(b => b)
            .ToArray();

        // Elona Dialog/Drama files are not in their LangCode subdirectory
        var fallback = books.First();
        var localized = books.FirstOrDefault(b => b.Contains($"/{lang}/")) ?? fallback;

        if (data.path.NormalizePath() != localized) {
            CwlMod.Log($"relocated sheet > {cachedBookName}:{Pattern}\n> {localized}");
        }

        PackageFileIterator.AddCachedPath(cachedBookName, localized);
        data.path = localized;
        return data.BuildList(sheet);
    }
}