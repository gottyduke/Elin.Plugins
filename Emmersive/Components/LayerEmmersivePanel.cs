using System.Collections.Generic;
using Cwl.API.Attributes;
using ReflexCLI.Attributes;
using UnityEngine;
using YKF;

namespace Emmersive.Components;

[ConsoleCommandClassCustomizer("em")]
internal class LayerEmmersivePanel : YKLayer<LayerCreationData>
{
    private static Vector2 _browsedPosition = Vector2.zero;
    private static string _lastOpenedTab = "";

    private readonly List<TabEmmersiveBase> _tabs = [];
    private bool _resetHyp;
    public override string Title => "Elin with AI";
    public override Rect Bound => FitWindow();

    public static LayerEmmersivePanel? Instance { get; private set; }

    public override void OnLayout()
    {
        Instance = this;

        if (Lang.setting.hyphenation) {
            Lang.setting.hyphenation = false;
            _resetHyp = true;
        }

        _tabs.Add(CreateTab<TabAiService>("em_ui_tab_ai_service", "em_tab_ai_service"));

        if (EClass.core.IsGameStarted) {
            _tabs.Add(CreateTab<TabWhitelist>("em_ui_tab_whitelist", "em_tab_whitelist"));
            _tabs.Add(CreateTab<TabSystemPrompt>("em_ui_tab_prompts", "em_tab_prompt_setting"));
            _tabs.Add(CreateTab<TabCharaPrompt>("em_ui_tab_characters", "em_tab_chara_prompts"));
            _tabs.Add(CreateTab<TabCharaRelations>("em_ui_tab_relations", "em_tab_chara_relations"));
        }

        _tabs.Add(CreateTab<TabDebugPanel>("em_ui_tab_debug", "em_tab_debug_panel"));
    }

    public override void OnAfterAddLayer()
    {
        base.OnAfterAddLayer();

        if (!Data.StartingTab.IsEmpty()) {
            _lastOpenedTab = Data.StartingTab;
        }

        Window.SwitchContent(_lastOpenedTab);

        Window.transform.localPosition = _browsedPosition;
    }

    public override void OnKill()
    {
        OnLayoutConfirm();

        if (Window != null && Window.CurrentContent != null) {
            _lastOpenedTab = Window.CurrentContent.name;
        }

        if (_resetHyp) {
            Lang.setting.hyphenation = true;
        }

        Instance = null;
    }

    public void Reopen()
    {
        ui.RemoveLayer(this);
        OpenPanelSesame(_lastOpenedTab);
    }

    public void OnLayoutConfirm()
    {
        foreach (var tab in _tabs) {
            tab.OnLayoutConfirm();
        }

        _browsedPosition = Window.transform.localPosition;
    }

    [ConsoleCommand("open")]
    [CwlContextMenu("em_ui_open_sesame")]
    internal static void OpenPanelSesame(string targetTab = "")
    {
        YK.CreateLayer<LayerEmmersivePanel, LayerCreationData>(new(targetTab));
    }

    private static Rect FitWindow()
    {
        var scaler = ui.canvasScaler.scaleFactor;
        var size = new Vector2(Screen.width / 1.5f, Screen.height / 1.5f) / scaler;
        return new(Vector2.zero, size);
    }
}