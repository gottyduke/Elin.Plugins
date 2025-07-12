using UnityEngine.EventSystems;

namespace ViewerMinus.API;

public interface IDraggable : IBeginDragHandler, IDragHandler, IEndDragHandler
{
    bool IsDragging { get; protected set; }
}