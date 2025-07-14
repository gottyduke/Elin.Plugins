using CustomizerMinus.API;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using UnityEngine;
using YKF;

namespace CustomizerMinus.Components;

internal class LayerCmmPartPicker : YKLayer<LayerCreationData>
{
    public override string Title => $"pcc_{Data.IdPartsSet}".Loc();
    public override Rect Bound => new(Vector2.zero, new(460f, 540f));

    public override void OnLayout()
    {
        this.StartDeferredCoroutine(AdjustRect);

        var count = PCC.GetAvailableParts(Data.UiPcc.pcc.GetBodySet(), Data.IdPartsSet).Count;
        var tab = CreateTab<TabCmmPartPicker>($"{Title} ({count})", $"cmm_tab_{Data.IdPartsSet}");

        if (TabCmmPartPicker.BrowsedPositions.TryGetValue(Data.IdPartsSet, out var pos)) {
            tab.GetComponentInParent<UIScrollView>().normalizedPosition = pos;
        }
    }

    private void AdjustRect()
    {
        var layer = EMono.ui.layers.Find(l => l is LayerEditPCC);
        if (layer == null) {
            return;
        }

        var window = layer.windows[0].RectTransform;
        transform.localPosition = window.localPosition;
    }
}