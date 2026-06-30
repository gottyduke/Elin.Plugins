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