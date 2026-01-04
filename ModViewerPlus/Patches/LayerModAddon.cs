using HarmonyLib;
using UnityEngine;
using ViewerMinus.API;
using ViewerMinus.Components;

namespace ViewerMinus.Patches;

[HarmonyPatch]
internal class LayerModAddon
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LayerMod), nameof(LayerMod.OnInit))]
    internal static void OnLayerModInit(LayerMod __instance)
    {
        __instance.windows[0].AddBottomButton("mvm_ui_save", ModListManager.SaveModList);
        __instance.windows[0].AddBottomButton("mvm_ui_load", ModListManager.LoadModList);
        __instance.windows[0].AddBottomButton("mvm_ui_remove", ModListManager.RemoveModList);

        // EA 23.253 integrated mod viewer
        if (!Core.Instance.version.IsBelow(0, 23, 253)) {
            return;
        }

        var vlg = __instance.list._layoutItems;
        vlg.GetOrCreate<DragController>();

        foreach (Transform mod in vlg.transform) {
            mod.GetOrCreate<DragBehaviour>();
        }
    }
}