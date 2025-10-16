using System.Collections.Generic;
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

    private readonly List<TabEmmersiveBase> _tabs = [];
    public override string Title => "Elin Immersive Talks";
    public override Rect Bound => FitWindow();

    public static LayerEmmersivePanel? Instance { get; private set; }

    public override void OnLayout()
    {
        Instance = this;

        _tabs.Add(CreateTab<TabAiService>("em_ui_tab_ai_service", "em_tab_ai_service"));
        _tabs.Add(CreateTab<TabPromptSetting>("em_ui_tab_prompts", "em_tab_prompt_setting"));
    }

    public override void OnAfterAddLayer()
    {
        base.OnAfterAddLayer();

        Window.transform.localPosition = _browsedPosition;
    }

    public override void OnKill()
    {
        OnLayoutConfirm();
        Instance = null;
    }

    public void Reopen()
    {
        ui.RemoveLayer(this);
        OpenPanelSesame();
    }

    public void OnLayoutConfirm()
    {
        foreach (var tab in _tabs) {
            tab.OnLayoutConfirm();
        }

        _browsedPosition = Window.transform.localPosition;

        ApiPoolSelector.Instance.SaveServices();
    }

    [ConsoleCommand("open")]
    [CwlContextMenu("em_ui_open_sesame")]
    internal static void OpenPanelSesame()
    {
        YK.CreateLayer<LayerEmmersivePanel, LayerCreationData>(new());
    }

    private static Rect FitWindow()
    {
        var scaler = ui.canvasScaler.scaleFactor;
        var size = new Vector2(Screen.width / 1.5f, Screen.height / 1.5f) / scaler;
        return new(Vector2.zero, size);
    }
}