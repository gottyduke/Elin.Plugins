using System.Linq;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class GetOrAddPersonPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DramaSequence), nameof(DramaSequence.GetActor))]
    internal static void OnGetOrAddActor(DramaSequence __instance, ref DramaActor __result, string id)
    {
        if (__result.id == id) {
            return;
        }

        if (__result.id == "tg" && DramaManager.TG?.id == id) {
            __instance.actors[id] = __result;
            return;
        }

        if (!EMono.sources.charas.map.TryGetValue(id, out var row)) {
            return;
        }

        var person = new Person(id);

        if (EClass.game.cards.globalCharas.Values.LastOrDefault(gc => gc.id == id) is { } chara) {
            person.SetChara(chara);
        } else {
            person.name = row.GetName();
            if (Portrait.allIds.Contains($"UN_{id}.png")) {
                person.idPortrait = $"UN_{id}";
            }
        }

        __result = __instance.AddActor(id, person);
    }
}