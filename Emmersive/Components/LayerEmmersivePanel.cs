using Cwl.API.Attributes;
using ReflexCLI.Attributes;
using UnityEngine;
using YKF;

namespace Emmersive.Components;

[ConsoleCommandClassCustomizer("em")]
internal class LayerEmmersivePanel : YKLayer<LayerCreationData>
{
    public override string Title => "Elin Immersive Talks";
    public override Rect Bound => new(Vector2.zero, new(800f, 600f));
    public static LayerEmmersivePanel? Instance { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public override void OnLayout()
    {
        CreateTab<TabAiService>("em_ui_tab_ai_service", "em_tab_ai_service");
    }

    [ConsoleCommand("open_panel")]
    [CwlContextMenu("Emmersive")]
    private static void OpenPanelSesame()
    {
        YK.CreateLayer<LayerEmmersivePanel, LayerCreationData>(new());
    }
}