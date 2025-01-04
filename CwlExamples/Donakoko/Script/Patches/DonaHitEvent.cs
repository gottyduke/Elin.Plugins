using System.Linq;
using Dona.Stats;
using Dona.Traits;
using HarmonyLib;

namespace Dona.Patches;

[HarmonyPatch]
internal class DonaHitEvent : EClass
{
    private static bool _skip;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AttackProcess), nameof(AttackProcess.Perform))]
    internal static void OnDonaAttack(AttackProcess __instance)
    {
        if (!__instance.hit || __instance.TC is not Chara target) {
            return;
        }

        if (__instance.CC.trait is not TraitDonakoko dona) {
            return;
        }

        var images = _map.Cards.Where(c => c.HasCondition<ConDonaAfterImage>());
        if (images.Count() >= (DonaConfig.ImageLimit?.Value ?? 2)) {
            return;
        }

        var chance = DonaConfig.ImageChance?.Value ?? 10;
        if (chance < rnd(30)) {
            return;
        }

        if (!_skip) {
            dona.TakePhoto(target);
        }

        _skip = !_skip;
    }
}