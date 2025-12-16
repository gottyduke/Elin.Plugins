using System.Collections.Generic;
using Cwl.API.Attributes;
using UnityEngine;
using YKF;
using Cwl.Helper.String;

namespace ElinTogether.Components;

internal class LayerElinTogether : YKLayer<LayerCreationData>
{
    private static Vector2 _browsedPosition = Vector2.zero;
    private static string _lastOpenedTab = "";

    private readonly List<TabEmpBase> _tabs = [];
    private bool _resetHyp;
    public override string Title => "Elin Together";
    public override Rect Bound => FitWindow();

    public static LayerElinTogether? Instance { get; private set; }

    public override void OnLayout()
    {
        Instance = this;

        if (Lang.setting.hyphenation) {
            Lang.setting.hyphenation = false;
            _resetHyp = true;
        }

        _tabs.Add(CreateTab<TabLobbyBrowser>("emp_ui_tab_lobby", "emp_tab_lobby"));
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
        if (Window != null) {
            _browsedPosition = Window.transform.localPosition;

            if (Window.CurrentContent != null) {
                _lastOpenedTab = Window.CurrentContent.name;
            }
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

    [CwlContextMenu("Elin Together")]
    private static void OpenInternal()
    {
        OpenPanelSesame();
    }

    public static void OpenPanelSesame(string targetTab = "")
    {
        YK.CreateLayer<LayerElinTogether, LayerCreationData>(new(targetTab));
    }

    private static Rect FitWindow()
    {
        var scaler = ui.canvasScaler.scaleFactor;
        var size = new Vector2(Screen.width / 1.5f, Screen.height / 1.5f) / scaler;
        return new(Vector2.zero, size);
    }
}