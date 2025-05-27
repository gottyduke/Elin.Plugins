using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Cwl.Patches.UIs;

[HarmonyPatch]
internal class RefitDialogChoicePatch
{
    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DramaActor), nameof(DramaActor.Talk))]
    internal static void OnSetTransform(DramaActor __instance, List<DramaChoice> choices)
    {
        var scaler = 5f * EMono.screen.Zoom;

        foreach (var choice in choices) {
            var csf = choice.button.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.enabled = true;

            var text = choice.button.mainText;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var size = text.rectTransform.sizeDelta;
            text.rectTransform.sizeDelta = size with { x = choice.button.Rect().sizeDelta.x - 10f * scaler, y = size.y + scaler };
        }

        __instance.dialog.transChoices.GetComponent<VerticalLayoutGroup>().spacing = scaler;
    }
}