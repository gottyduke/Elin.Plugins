using System;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine.UI;

namespace CustomizerMinus.Patches;

[HarmonyPatch]
internal class PccWindowAddon
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LayerEditPCC), nameof(LayerEditPCC.Activate))]
    internal static void OnActivateLayer(LayerEditPCC __instance)
    {
        try {
            var window = __instance.windows[1];
            var content = window.transform.GetFirstNestedChildWithName("Content View/Panel Screen");
            if (content == null) {
                return;
            }

            var deltaBar = content.GetFirstChildWithName("Slider delta");
            if (deltaBar != null) {
                deltaBar.GetComponent<Slider>().image.sprite = "cmm_delta".LoadSprite();
            }

            var scaleBar = content.GetFirstChildWithName("Slider");
            if (scaleBar != null) {
                scaleBar.GetComponent<Slider>().image.sprite = "cmm_scale".LoadSprite();
            }

            var rotateLeft = content.GetFirstChildWithName("UIButton left");
            if (rotateLeft != null) {
                rotateLeft.Rect().sizeDelta = rotateLeft.Rect().sizeDelta * 2f;
            }

            var rotateRight = content.GetFirstChildWithName("UIButton right");
            if (rotateRight != null) {
                rotateRight.Rect().sizeDelta = rotateRight.Rect().sizeDelta * 2f;
            }
        } catch (Exception ex) {
            CmmMod.Log($"failed to patch edit pcc layer {ex}");
            // noexcept
        }
    }
}