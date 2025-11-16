using Cwl.API.Attributes;
using Cwl.Helper.Unity;
using ElinTogether.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ElinTogether.Patches;

internal class TitleButtonPatch
{
    [CwlSceneInitEvent(Scene.Mode.Title, true)]
    internal static void RegisterTitleButton()
    {
        var title = EMono.ui.GetLayer<LayerTitle>();
        if (title == null) {
            return;
        }

        var grid = title.GetComponentInChildren<GridLayoutGroup>();
        if (grid == null) {
            return;
        }

        var button = grid.transform.GetFirstChildWithName("UIButton");
        if (button == null) {
            return;
        }

        var empButtonGo = Object.Instantiate(button, grid.transform);
        empButtonGo.SetSiblingIndex(button.GetSiblingIndex() + 2);

        var empButton = empButtonGo.GetComponent<UIButton>();
        empButton.mainText.text = "Elin Together";
        empButton.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
        empButton.onClick.AddListener(() => LayerElinTogether.OpenPanelSesame());
    }
}