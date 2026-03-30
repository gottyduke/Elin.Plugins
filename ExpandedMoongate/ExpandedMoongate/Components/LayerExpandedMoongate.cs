using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using Exm.API;
using Exm.Components.Tabs;
using Exm.Helper;
using ReflexCLI.Attributes;
using UnityEngine;
using YKF;

namespace Exm.Components;

[ConsoleCommandClassCustomizer("exm")]
internal class LayerExpandedMoongate : YKLayer<LayerCreationData>
{
    private static Vector2 _browsedPosition = Vector2.zero;
    private static string _lastOpenedTab = "";
    private readonly bool _resetHyp = Lang.setting.hyphenation;

    private readonly List<TabExMoongateBase> _tabs = [];
    public override string Title => "Expanded Moongate Server";
    public override Rect Bound => UIHelper.FitWindow();
    public static LayerExpandedMoongate? Instance { get; private set; }

    public override void OnLayout()
    {
        Instance = this;

        if (_resetHyp) {
            Lang.setting.hyphenation = false;
        }

        _tabs.Add(CreateTab<TabMapBrowser>("exm_ui_tab_map_browser", "exm_tab_map_browser"));
        _tabs.Add(CreateTab<TabMapHistory>("exm_ui_tab_map_history", "exm_tab_map_history"));
        _tabs.Add(CreateTab<TabDebugPanel>("exm_ui_tab_debug_panel", "exm_tab_debug_panel"));

        if (_enableModeration) {
            _tabs.Add(CreateTab<TabModeration>("exm_ui_tab_moderation", "exm_tab_moderation"));
        }
    }

    public override void OnAfterAddLayer()
    {
        base.OnAfterAddLayer();

        if (!Data.StartingTab.IsEmptyOrNull) {
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

    [CwlContextMenu("exm_ui_open_sesame")]
    private static void OpenInternal()
    {
        OpenPanelSesame();
    }

    [ConsoleCommand("open")]
    internal static void OpenPanelSesame(string targetTab = "")
    {
        YK.CreateLayer<LayerExpandedMoongate, LayerCreationData>(new(targetTab));
    }
}