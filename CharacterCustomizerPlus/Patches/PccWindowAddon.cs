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

            var rotateScaler = CmmConfig.RotateButtonScale.Value;
            var rotateLeft = content.GetFirstChildWithName("UIButton left");
            if (rotateLeft != null) {
                rotateLeft.Rect().sizeDelta *= rotateScaler;
            }

            var rotateRight = content.GetFirstChildWithName("UIButton right");
            if (rotateRight != null) {
                rotateRight.Rect().sizeDelta *= rotateScaler;
            }

            if (!CmmConfig.EnableSliderIcon.Value) {
                return;
            }

            var sliderScaler = CmmConfig.SliderIconScale.Value;
            var deltaBar = content.GetFirstChildWithName("Slider delta");
            if (deltaBar != null) {
                var image = deltaBar.GetComponent<Slider>().image;
                image.sprite = "cmm_delta".LoadSprite();
                image.rectTransform.sizeDelta *= sliderScaler;
            }

            var scaleBar = content.GetFirstChildWithName("Slider");
            if (scaleBar != null) {
                var image = scaleBar.GetComponent<Slider>().image;
                image.sprite = "cmm_scale".LoadSprite();
                image.rectTransform.sizeDelta *= sliderScaler;
            }
        } catch (Exception ex) {
            CmmMod.Log($"failed to patch edit pcc layer {ex}");
            // noexcept
        }
    }
}