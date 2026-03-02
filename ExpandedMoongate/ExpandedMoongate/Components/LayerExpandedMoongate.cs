using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using EGate.Components.Tabs;
using ReflexCLI.Attributes;
using UnityEngine;
using YKF;

namespace EGate.Components;

[ConsoleCommandClassCustomizer("eg")]
internal class LayerExpandedMoongate : YKLayer<LayerCreationData>
{
    private static Vector2 _browsedPosition = Vector2.zero;
    private static string _lastOpenedTab = "";
    private readonly bool _resetHyp = Lang.setting.hyphenation;

    private readonly List<TabExMoongateBase> _tabs = [];
    public override string Title => "Expanded Moongate Server";
    public override Rect Bound => FitWindow();

    public static LayerExpandedMoongate? Instance { get; private set; }

    public override void OnLayout()
    {
        Instance = this;

        if (_resetHyp) {
            Lang.setting.hyphenation = false;
        }

        _tabs.Add(CreateTab<TabMapBrowser>("eg_ui_tab_map_browser", "eg_tab_map_browser"));
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

    [CwlContextMenu("eg_ui_open_sesame")]
    private static void OpenInternal()
    {
        OpenPanelSesame();
    }

    [ConsoleCommand("open")]
    internal static void OpenPanelSesame(string targetTab = "")
    {
        YK.CreateLayer<LayerExpandedMoongate, LayerCreationData>(new(targetTab));
    }

    private static Rect FitWindow()
    {
        var scaler = ui.canvasScaler.scaleFactor;
        var size = new Vector2(Screen.width / 1.5f, Screen.height / 1.5f) / scaler;
        return new(Vector2.zero, size);
    }
}