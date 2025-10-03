using System.Linq;
using UnityEngine;
using ViewerMinus.API;

namespace ViewerMinus.Components;

public class DragController : EMono
{
    private int _dragThreshold;
    public static DragBehaviour? CurrentDragging { get; internal set; }

    private void Awake()
    {
        CurrentDragging = null;
        _dragThreshold = core.eventSystem.pixelDragThreshold;
        core.eventSystem.pixelDragThreshold = 10;
    }

    private void OnDestroy()
    {
        core.eventSystem.pixelDragThreshold = _dragThreshold;
    }

    public void EndDrag()
    {
        var layer = LayerMod.Instance;
        if (layer == null || CurrentDragging == null) {
            return;
        }

        var begin = CurrentDragging.BeginIndex;
        var end = CurrentDragging.transform.GetSiblingIndex();
        var diff = end - begin;

        if (diff == 0) {
            return;
        }

        SE.Tab();
        layer.manager.packages.Move(CurrentDragging.GetComponent<ItemMod>().package, diff);
        layer.textRestart.SetActive(true);

        ModListManager.RefreshList();
    }

#if DEBUG
    private static Rect _windowRect = new(10f, 10f, 200f, 200f);
    private static readonly int _windowId = ModInfo.Guid.GetHashCode();

    private void OnGUI()
    {
        _windowRect = GUILayout.Window(_windowId, _windowRect, DrawDebugThingy, ModInfo.Name);
    }

    private static void DrawDebugThingy(int windowId)
    {
        var builtin = core.mods.packages.Count(p => p.builtin) - 1;
        GUILayout.Label($"Dragging : {CurrentDragging?.BeginIndex + builtin}");
        GUILayout.Label($"Hovering : {HoverBehaviour.CurrentHovering?.transform.GetSiblingIndex() + builtin}");
    }
#endif
}