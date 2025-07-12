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
        var vlg = __instance.list._layoutItems;
        vlg.gameObject.AddComponent<DragController>();
        foreach (Transform mod in vlg.transform) {
            mod.gameObject.AddComponent<DragBehaviour>();
        }

        __instance.windows[0].AddBottomButton("mvm_ui_save", ModListManager.SaveModList);
        __instance.windows[0].AddBottomButton("mvm_ui_load", ModListManager.LoadModList);
        __instance.windows[0].AddBottomButton("mvm_ui_remove", ModListManager.RemoveModList);
    }
}