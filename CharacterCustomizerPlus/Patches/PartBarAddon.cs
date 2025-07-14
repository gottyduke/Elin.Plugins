using System;
using CustomizerMinus.API;
using CustomizerMinus.Components;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using YKF;
using Object = UnityEngine.Object;

namespace CustomizerMinus.Patches;

[HarmonyPatch]
internal class PartBarAddon
{
    internal static float scaler = ELayer.ui.canvasScaler.scaleFactor;
    private static ButtonGeneral? _sharedBtn;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIPCC), nameof(UIPCC.AddPart))]
    internal static void OnLayerEditPccInit(UIPCC __instance, string idPartsSet)
    {
        var bar = __instance.layoutParts.transform.GetLastChild();

        try {
            var pcc = bar.GetComponent<UIItemPCC>();
            var uiText = pcc.slider.textMain;
            if (uiText == null) {
                return;
            }

            if (_sharedBtn == null) {
                _sharedBtn = bar.GetFirstChildWithName("ButtonSimple icon")!.GetComponent<ButtonGeneral>();
                TabCmmPartPicker.InitPrefabCell(_sharedBtn);
            }

            var rect = uiText.rectTransform;
            rect.sizeDelta = rect.sizeDelta with { y = _sharedBtn.Rect().sizeDelta.y };
            rect.localPosition = rect.localPosition with { x = rect.localPosition.x + 3f * scaler };
            uiText.alignment = TextAnchor.MiddleRight;

            var newText = Util.Instantiate(uiText, uiText).transform;
            newText.position = uiText.transform.position;
            newText.localScale = uiText.transform.localScale;
            newText.localPosition = new(-12f * scaler, 0f, 0f);

            var newBar = uiText.gameObject;
            Object.DestroyImmediate(uiText);

            var image = newBar.AddComponent<Image>();
            image.sprite = _sharedBtn.GetComponent<Image>().sprite;

            var newBtn = newBar.AddComponent<ButtonGeneral>();
            newBtn.transition = Selectable.Transition.SpriteSwap;
            newBtn.spriteState = _sharedBtn.spriteState;

            newBtn.tooltip = new() {
                enable = true,
                lang = $"id: {idPartsSet}",
                id = $"cmm_tooltip_{idPartsSet}",
                offset = new(-20f * scaler, -10f),
            };
            newBtn.SetOnClick(() =>
                YK.CreateLayer<LayerCmmPartPicker, LayerCreationData>(new(idPartsSet, __instance)));
        } catch (Exception ex) {
            CmmMod.Log($"failed to transform part bar {idPartsSet}\n{ex}");
            // noexcept
        }
    }
}