using System.Collections;
using CustomizerMinus.API;
using Cwl.LangMod;
using UnityEngine;
using YKF;

namespace CustomizerMinus.Components;

internal class LayerCmmPartPicker : YKLayer<LayerCreationData>
{
    public override string Title => $"pcc_{Data.IdPartsSet}".Loc();
    public override Rect Bound => FitWindow();

    public override void OnLayout()
    {
        var count = PCC.GetAvailableParts(Data.UiPcc.pcc.GetBodySet(), Data.IdPartsSet).Count;
        var tab = CreateTab<TabCmmPartPicker>($"{Title} ({count})", $"cmm_tab_{Data.IdPartsSet}");

        if (TabCmmPartPicker.BrowsedPositions.TryGetValue(Data.IdPartsSet, out var pos)) {
            tab.GetComponentInParent<UIScrollView>().normalizedPosition = pos;
        }

        StartCoroutine(AdjustRect());
    }

    private IEnumerator AdjustRect()
    {
        yield return null;

        var layer = EMono.ui.layers.Find(l => l is LayerEditPCC);
        if (layer == null) {
            yield break;
        }

        var window = layer.windows[0].RectTransform;
        var pos = window.localPosition;
        var sizeDelta = (windows[0].RectTransform.sizeDelta - window.sizeDelta) / 2f;
        transform.localPosition = pos with { x = pos.x - sizeDelta.x };
    }

    private static Rect FitWindow()
    {
        Vector2 baseSize = new(76f, 540f);
        var cellsWidth = CmmConfig.MaxPartsPerRow.Value * CmmConfig.PartCellWidth.Value;
        return new(Vector2.zero, baseSize with { x = baseSize.x + cellsWidth });
    }
}