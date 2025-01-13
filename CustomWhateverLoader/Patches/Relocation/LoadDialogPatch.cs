using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Runtime;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Relocation;

[HarmonyPatch]
internal class LoadDialogPatch
{
    private const string CacheEntry = "Dialog";
    private const string Pattern = "*.xlsx";

    private static readonly List<ExcelData> _cached = [];

    [HarmonyTargetMethods]
    internal static IEnumerable<MethodInfo> DialogBuildMap()
    {
        return [
            AccessTools.Method(typeof(DramaCustomSequence), nameof(DramaCustomSequence.HasTopic)),
            AccessTools.Method(typeof(Lang), nameof(Lang.GetDialog)),
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnLoadIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Callvirt, nameof(ExcelData.BuildMap)))
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate(BuildRelocatedMap))
            .InstructionEnumeration();
    }

    [Time]
    private static void BuildRelocatedMap(ExcelData data, string sheetName)
    {
        data.BuildMap(sheetName);

        foreach (var cache in _cached) {
            cache.BuildMap(sheetName);
            foreach (var (topic, cells) in cache.sheets[sheetName].map) {
                if (topic.IsEmpty()) {
                    continue;
                }

                data.sheets[sheetName].map.TryAdd(topic, cells);
            }
        }
    }

    [Time]
    internal static IEnumerator LoadAllDialogs()
    {
        var dialogs = PackageIterator.GetRelocatedFilesFromPackage("Dialog/dialog.xlsx");

        foreach (var book in dialogs) {
            _cached.Add(new(book.FullName));
            CwlMod.Log<ExcelData>("cwl_preload_dialog".Loc(book.ShortPath()));
        }

        yield break;
    }
}