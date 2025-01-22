using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.Patches.Relocation;
using MethodTimer;

namespace Cwl;

internal partial class DataLoader
{
    [Time]
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