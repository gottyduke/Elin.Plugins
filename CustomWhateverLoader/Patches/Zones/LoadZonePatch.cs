using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Zones;

internal class LoadZonePatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverridesGetter(typeof(Zone), nameof(Zone.pathExport));
    }

    [HarmonyPostfix]
    internal static void OnLoadCustomZone(Zone __instance, ref string __result)
    {
        if (__instance.idExport.IsEmptyOrNull && !__instance.source.tag.Contains("addMap")) {
            return;
        }

        // we use Maps instead of Map or Map Piece to avoid tangling with saved moongate maps
        string[] fileNames = [
            $"Maps/{__instance.idExport}.z",
            $"Maps/{__instance.ZoneFullName}.z",
        ];
        var candidate = fileNames
            .SelectMany(PackageIterator.GetRelocatedFilesFromPackage)
            .LastOrDefault();

        if (candidate is not null) {
            __result = candidate.FullName;
        }
    }
}