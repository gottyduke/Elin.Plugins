using UnityEngine.EventSystems;
using ViewerMinus.API;

namespace ViewerMinus.Components;

public class DragBehaviour : HoverBehaviour, IDraggable
{
    public DragController? Controller => GetComponentInParent<DragController>();
    public int BeginIndex { get; private set; }
    public bool IsDragging { get; set; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDragging = true;
        DragController.CurrentDragging = this;
        BeginIndex = transform.GetSiblingIndex();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // *wave*
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        Controller?.EndDrag();
        DragController.CurrentDragging = null;
    }

    public override void Enter(HoverBehaviour ho)
    {
        base.Enter(ho);

        var current = DragController.CurrentDragging;
        if (current == null) {
            return;
        }

        current.transform.SetSiblingIndex(transform.GetSiblingIndex());
    }
}