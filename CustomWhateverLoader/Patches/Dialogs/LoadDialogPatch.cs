using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class LoadDialogPatch
{
    internal static readonly List<ExcelData> Cached = [];
    private static readonly Dictionary<string, Dictionary<string, ExcelData.Sheet>> _built = [];

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(DramaCustomSequence), nameof(DramaCustomSequence.HasTopic)),
            #if NIGHTLY
            AccessTools.Method(typeof(Lang), nameof(Lang.GetDialogSheet)),
            #else
            AccessTools.Method(typeof(Lang), nameof(Lang.GetDialog)),
            #endif
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnLoadIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Callvirt, nameof(ExcelData.BuildMap)))
            .EnsureValid("build map")
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate(BuildRelocatedMap))
            .InstructionEnumeration();
    }

    [Time]
    private static void BuildRelocatedMap(ExcelData data, string sheetName)
    {
        // using caching here will disable vanilla dialog hot reload
        // I doubt anyone uses it, not with CWL anyway, hehe
        if (!CwlConfig.CacheTalks) {
            MergeDialogs(data, sheetName);
            return;
        }

        if (_built.TryGetValue(sheetName, out var built)) {
            data.sheets = built;
        } else {
            MergeDialogs(data, sheetName);
            _built[sheetName] = data.sheets;
        }
    }

    private static void MergeDialogs(ExcelData data, string sheetName)
    {
        data.BuildMap(sheetName);

        foreach (var cache in Cached) {
            cache.BuildMap(sheetName);
            foreach (var (topic, cells) in cache.sheets[sheetName].map) {
                if (topic.IsEmptyOrNull) {
                    continue;
                }

                data.sheets[sheetName].map[topic] = cells;
            }
        }
    }
}