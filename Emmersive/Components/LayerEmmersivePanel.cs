using Cwl.API.Attributes;
using YKF;

namespace Emmersive.Components;

internal class LayerEmmersivePanel : YKLayer<LayerCreationData>
{
    public override string Title => "Elin Immersive";

    public override void OnLayout()
    {
        CreateTab<TabEmmersiveTalk>("Talk", "em_tab_talk");
    }

    [CwlContextMenu("Emmersive")]
    private static void OpenPanel()
    {
        YK.CreateLayer<LayerEmmersivePanel, LayerCreationData>(new());
    }
}