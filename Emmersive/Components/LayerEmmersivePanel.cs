using Cwl.API.Attributes;
using Emmersive.API.Services;
using ReflexCLI.Attributes;
using UnityEngine;
using YKF;

namespace Emmersive.Components;

[ConsoleCommandClassCustomizer("em")]
internal class LayerEmmersivePanel : YKLayer<LayerCreationData>
{
    private static Vector2 _browsedPosition = Vector2.zero;
    public override string Title => "Elin Immersive Talks";
    public override Rect Bound => new(Vector2.zero, new(Screen.width / 1.5f, Screen.height / 1.5f));

    public static LayerEmmersivePanel? Instance { get; private set; }

    public override void OnLayout()
    {
        Instance = this;

        CreateTab<TabAiService>("em_ui_tab_ai_service", "em_tab_ai_service");
        CreateTab<TabPromptSetting>("em_ui_tab_prompts", "em_tab_prompt_setting");
    }

    public override void OnAfterAddLayer()
    {
        base.OnAfterAddLayer();

        Window.transform.localPosition = _browsedPosition;
    }

    public override void OnKill()
    {
        Instance = null;

        _browsedPosition = Window.transform.localPosition;

        ApiPoolSelector.Instance.SaveServices();
    }

    public void Reopen()
    {
        Kill();
        OpenPanelSesame();
    }

    [ConsoleCommand("open")]
    [CwlContextMenu("Emmersive~")]
    internal static void OpenPanelSesame()
    {
        YK.CreateLayer<LayerEmmersivePanel, LayerCreationData>(new());
    }
}