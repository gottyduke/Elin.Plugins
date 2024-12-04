using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Patches;

[HarmonyPatch]
internal class LoadLangPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Msg), nameof(Msg.GetGameText))]
    internal static void OnGetGameText(IDictionary<string, object> source, string key, object fallback, ref object __result)
    {
    }
}