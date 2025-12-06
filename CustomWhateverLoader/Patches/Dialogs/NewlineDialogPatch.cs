using System;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class NewlineDialogPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lang), nameof(Lang.GetDialog))]
    internal static void OnGetUniqueDialog(string[] __result, string idSheet)
    {
        for (var i = 0; i < __result.Length; ++i) {
            __result[i] = Regex.Replace(__result[i], @"<br\s*/?>", Environment.NewLine, RegexOptions.IgnoreCase);
        }
    }
}