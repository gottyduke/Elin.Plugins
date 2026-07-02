using System.Collections;
using UnityEngine;
using YKF;

namespace Emmersive.Components;

internal class LayerMemoryEditor : LayerEmmersiveBase<LayerMemoryCreationData>
{
    public override string Title => "em_ui_tab_memory".lang();

    public static LayerMemoryEditor? Instance { get; private set; }

    public override void OnLayout()
    {
        base.OnLayout();

        Instance = this;

        CreateTab<TabMemoryEditor>("em_ui_tab_memory", "em_tab_memory");

        StartCoroutine(AdjustRect());
    }

    private IEnumerator AdjustRect()
    {
        yield return null;

        transform.position -= new Vector3(25f, 25f);
    }

    public void Reopen()
    {
        ui.RemoveLayer(this);
        YK.CreateLayer<LayerMemoryEditor, LayerMemoryCreationData>(Data);
    }

    public override void OnKill()
    {
        Instance = null;

        LayerEmmersivePanel.Instance?.Reopen();
    }
}