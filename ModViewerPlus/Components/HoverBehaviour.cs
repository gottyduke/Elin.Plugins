using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace ViewerMinus.Components;

public class HoverBehaviour : EMono, IPointerEnterHandler, IPointerExitHandler
{
    public static readonly List<HoverBehaviour> HoverHierarchy = [];
    public static HoverBehaviour? CurrentHovering => HoverHierarchy.TryGet(HoverHierarchy.Count - 1);

    public bool IsHovering { get; set; }

    private void OnDisable()
    {
        Leave(this);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        Enter(this);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        Leave(this);
    }

    public virtual void Enter(HoverBehaviour ho)
    {
        IsHovering = true;
        HoverHierarchy.Add(ho);
    }

    public virtual void Leave(HoverBehaviour ho)
    {
        IsHovering = false;
        HoverHierarchy.Remove(ho);
    }
}