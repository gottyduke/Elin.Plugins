﻿using Cwl.Helper.FileUtil;
using HarmonyLib;

namespace Cwl.Patches;

[HarmonyPatch]
internal class ReloadLangPatch
{
    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Core), nameof(Core.SetLang))]
    internal static void OnSetLang()
    {
        PackageIterator.ClearCache();
        DataLoader.MergeGodTalk();
    }
}