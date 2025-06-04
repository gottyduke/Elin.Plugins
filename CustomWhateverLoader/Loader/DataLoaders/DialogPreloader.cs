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
            CwlMod.CurrentLoading = $"[CWL] Dialog/{book.ShortPath()}";

            LoadDialogPatch.Cached.Add(new(book.FullName));
            CwlMod.Log<DataLoader>("cwl_preload_dialog".Loc(book.ShortPath()));
        }
    }
}