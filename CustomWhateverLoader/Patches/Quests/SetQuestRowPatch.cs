#if DEBUG
using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Patches.Sources;
using HarmonyLib;

namespace Cwl.Patches.Quests;

[HarmonyPatch]
internal class SetQuestRowPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SourceQuest), nameof(SourceQuest.SetRow))]
    internal static void OnSetRow(SourceQuest.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        var qualified = TypeQualifier.TryQualify<Quest>(r.type);
        if (qualified?.FullName is { } fullName) {
            r.type = fullName;
        }

        if (qualified == typeof(CustomQuest)) {
            CustomQuest.Managed[r.id] = r;
        }
    }
}
#endif