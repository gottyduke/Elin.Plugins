using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class LoadDialogPatch
{
    internal static readonly List<ExcelData> Cached = [];

    internal static IEnumerable<MethodInfo> TargetMethods()
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

        foreach (var cache in Cached) {
            cache.BuildMap(sheetName);
            foreach (var (topic, cells) in cache.sheets[sheetName].map) {
                if (topic.IsEmpty()) {
                    continue;
                }

                data.sheets[sheetName].map[topic] = cells;
            }
        }
    }
}