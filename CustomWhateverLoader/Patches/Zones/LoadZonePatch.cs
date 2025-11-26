using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Zones;

[HarmonyPatch]
internal class LoadZonePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverridesGetter(typeof(Zone), nameof(Zone.pathExport));
    }

    [HarmonyPostfix]
    internal static void OnLoadCustomZone(Zone __instance, ref string __result)
    {
        if (!__instance.source.tag.Contains("addFile") &&
            !__instance.idExport.IsEmpty() &&
            File.Exists(__result)) {
            return;
        }

        // we use Maps instead Map or Map Piece to avoid tangling with in game moongate stuff
        var zoneFullName = __instance.ZoneFullName;
        var fileName = $"Maps/{__instance.idExport}.z";
        var candidate = PackageIterator
            .GetRelocatedFilesFromPackage(fileName)
            .LastOrDefault();

        if (candidate is null) {
            return;
        }

        __result = candidate.FullName;
        CwlMod.Log<DramaManager>("cwl_relocate_zone".Loc(__instance.NameWithLevel, zoneFullName, candidate.ShortPath()));
    }
}