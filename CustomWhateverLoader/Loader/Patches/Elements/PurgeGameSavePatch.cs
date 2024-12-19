using System.Linq;
using Cwl.API;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Elements;

[HarmonyPatch]
internal class PurgeGameSavePatch
{
    private static HotItem? _held;

    // credits to 105gun
    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.Save))]
    private static void PurgeCustomElement()
    {
        if (EClass.player?.currentHotItem is not HotItemAct act ||
            CustomElement.All.All(r => act.id != r.id)) {
            return;
        }

        _held = act;
        EClass.player.currentHotItem = null;
        EClass.player.RefreshCurrentHotItem();
    }

    [Time]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Game), nameof(Game.Save))]
    private static void RestoreCustomElement()
    {
        if (EMono.core?.game is null ||
            EClass.player?.chara is null ||
            _held is null) {
            return;
        }

        EClass.player.currentHotItem = _held;
        EClass.player.RefreshCurrentHotItem();
        _held = null;
    }
}