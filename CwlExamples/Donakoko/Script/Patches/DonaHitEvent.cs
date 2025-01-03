using System.Linq;
using Dona.Stats;
using Dona.Traits;
using HarmonyLib;

namespace Dona.Patches;

[HarmonyPatch]
internal class DonaHitEvent : EClass
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AttackProcess), nameof(AttackProcess.Perform))]
    internal static void OnDonaAttack(AttackProcess __instance, bool __result)
    {
        if (__instance.CC.trait is not TraitDonakoko dona ||
            __instance.TC is not Chara target || 
            !__result) {
            return;
        }

        var image = _map.Cards.FirstOrDefault(c => c.HasCondition<ConDonaAfterImage>());
        if (image is not null) {
            return;
        }

        var chance = DonaConfig.ImageChance?.Value ?? 10;
        if (chance >= rnd(30)) {
            dona.TakePhoto(target);
        }
    }
}