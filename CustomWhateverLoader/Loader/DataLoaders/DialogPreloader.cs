using System.Collections.Generic;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;
using ReflexCLI.Attributes;

namespace Cwl;

internal partial class DataLoader
{
    internal static readonly List<ExcelData> CachedDialogs = [];

    [Time]
    [ConsoleCommand("load_dialog")]
    internal static void PreloadDialog()
    {
        CachedDialogs.Clear();

        var dialogs = PackageIterator.GetRelocatedFilesFromPackage("Dialog/dialog.xlsx");

        foreach (var book in dialogs) {
            var path = book.ShortPath();
            CwlMod.CurrentLoading = $"[CWL] Dialog/{path}";

            CachedDialogs.Add(new(book.FullName));
            CwlMod.Log<DataLoader>("cwl_preload_dialog".Loc(path));
        }
    }

    internal static void MergeDialogs(ExcelData data, string sheetName)
    {
        EnsureSheetExists(data, sheetName);

        foreach (var cache in CachedDialogs) {
            EnsureSheetExists(cache, sheetName);

            foreach (var (topic, cells) in cache.sheets[sheetName].map) {
                if (topic.IsEmptyOrNull) {
                    continue;
                }

                data.sheets[sheetName].map[topic] = cells;
            }
        }
    }

    private static void EnsureSheetExists(ExcelData data, string sheetName)
    {
        data.LoadBook();
        data.BuildMap(sheetName);
        if (data.book.GetSheet(sheetName) is null) {
            data.sheets[sheetName].list.Clear();
            data.sheets[sheetName].map.Clear();
        }
    }
}