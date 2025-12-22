using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class LoadDialogPatch
{
    private static readonly Dictionary<string, WeakReference<Dictionary<string, ExcelData.Sheet>>> _built =
        new(StringComparer.Ordinal);

    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Lang), nameof(Lang.GetDialogSheet))]
    internal static void OnLoadMergedDialog(string idSheet)
    {
        if (Lang.excelDialog is null) {
            var path = Path.Combine(CorePath.CorePackage.TextDialog, "dialog.xlsx");
            Lang.excelDialog = new(path) {
                path = path,
            };
        }

        // using caching here will disable vanilla dialog hot reload
        // I doubt anyone uses it, not with CWL anyway, hehe
        if (!CwlConfig.CacheTalks) {
            DataLoader.MergeDialogs(Lang.excelDialog, idSheet);
            return;
        }

        if (_built.TryGetValue(idSheet, out var builtRef) && builtRef.TryGetTarget(out var built)) {
            Lang.excelDialog.sheets = built;
        } else {
            DataLoader.MergeDialogs(Lang.excelDialog, idSheet);
            _built[idSheet] = new(Lang.excelDialog.sheets);
        }
    }

    [HarmonyPatch]
    internal class CachedDramaDialogPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DramaCustomSequence), nameof(DramaCustomSequence.HasTopic))]
        internal static bool OnGetCachedDialog(string idSheet, string idTopic, ref bool __result)
        {
            var sheet = Lang.GetDialogSheet(idSheet);
            __result = sheet.map.ContainsKey(idTopic);
            return false;
        }
    }
}