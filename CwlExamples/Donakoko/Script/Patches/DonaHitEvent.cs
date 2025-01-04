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
    internal static void OnDonaAttack(AttackProcess __instance)
    {
        // if not hit or target is not Chara
        if (!__instance.hit || __instance.TC is not Chara target) {
            return;
        }

        // if attacker is not dona
        if (__instance.CC.trait is not TraitDonakoko dona) {
            return;
        }

        // if existing images already at limit
        var images = _map.Cards.Where(c => c.HasCondition<ConDonaAfterImage>());
        if (images.Count() >= (DonaConfig.ImageLimit?.Value ?? 2)) {
            return;
        }

        // chance of taking a photo
        var chance = DonaConfig.ImageChance?.Value ?? 10;
        if (chance < rnd(100)) {
            return;
        }

        // take photo image
        dona.TakePhoto(target);
    }
}