using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Relocation;

[HarmonyPatch]
internal class LoadBookPatch
{
    private const string CacheEntry = "Text";
    private const string Pattern = "*.txt";

    [Time]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BookList), nameof(BookList.Init))]
    internal static void OnBookListInit(BookList __instance)
    {
        var sources = PackageIterator.GetLangModFilesFromPackage()
            .SelectMany(d => d.GetDirectories(CacheEntry))
            .SelectMany(d => d.GetDirectories())
            .ToArray();

        foreach (var category in BookList.dict.Keys) {
            var available = sources
                .Where(b => b.Name == category)
                .SelectMany(d => d.GetFiles(Pattern));

            foreach (var book in available) {
                using var sr = new StreamReader(book.FullName);

                var meta = sr.ReadLine()!.Split(',');
                var item = new BookList.Item {
                    title = meta[0],
                    author = meta.Length >= 2 && !meta[1].IsEmpty()
                        ? "nameAuthor".lang(meta[1])
                        : "unknownAuthor".lang(),
                    chance = meta.Length >= 3 ? meta[2].ToInt() : 100,
                    id = Path.GetFileNameWithoutExtension(book.Name),
                };

                BookList.dict[category][item.id] = item;
                PackageIterator.AddCachedPath($"{category}/{item.id}", book.FullName);

                CwlMod.Log<BookList>($"{category}: {book.Name}|{book.Directory?.Parent?.Parent?.Name}");
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIBook), nameof(UIBook.BuildPages))]
    internal static IEnumerable<CodeInstruction> OnBuildBookTextIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(
                    typeof(UIBook.Page))))
            .InsertAndAdvance(
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(BuildRelocatedPages),
                new(OpCodes.Stloc_0))
            .InstructionEnumeration();
    }

    [Time]
    private static string[] BuildRelocatedPages(string[] textArray, UIBook book)
    {
        var id = book.idFile;
        if (!textArray.IsEmpty()) {
            return textArray;
        }

        if (!PackageIterator.TryLoadFromPackageCache(id, out var cachedPath)) {
            return [];
        }

        CwlMod.Log<BookList>("cwl_relocate_book".Loc(id, Pattern, cachedPath.ShortPath()));
        return IO.LoadTextArray(cachedPath);
    }
}