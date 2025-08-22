//#define ENABLE_QUEST
#if ENABLE_QUEST
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
        if (qualified?.FullName is not null) {
            r.type = qualified.FullName;
        }

        if (r.drama.Length == 0) {
            return;
        }

        if (r.tags.Contains("autoStart")) {
            CustomQuest.Managed[r.id] = r;
        }
    }
}
#endif