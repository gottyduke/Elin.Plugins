using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.Patches.Dialogs;
using MethodTimer;
using ReflexCLI.Attributes;

namespace Cwl;

internal partial class DataLoader
{
    [Time]
    [ConsoleCommand("load_dialog")]
    internal static void PreloadDialog()
    {
        LoadDialogPatch.Cached.Clear();

        var dialogs = PackageIterator.GetRelocatedFilesFromPackage("Dialog/dialog.xlsx");

        foreach (var book in dialogs) {
            var path = book.ShortPath();
            CwlMod.CurrentLoading = $"[CWL] Dialog/{path}";

            LoadDialogPatch.Cached.Add(new(book.FullName));
            CwlMod.Log<DataLoader>("cwl_preload_dialog".Loc(path));
        }
    }
}