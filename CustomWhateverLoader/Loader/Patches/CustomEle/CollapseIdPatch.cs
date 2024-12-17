using System.Collections.Generic;
using HarmonyLib;

namespace Cwl.Loader.Patches.CustomEle;

[HarmonyPatch]
internal class CollapseIdPatch
{
    private static readonly Dictionary<int, int> _relocated = [];
}