using UnityEngine;

namespace Cwl.Helper.Extensions;

public static class UnityEventExt
{
    extension(Event @event)
    {
        public bool IsLeftMouseDown => @event is { type: EventType.MouseDown, button: 0 };
        public bool IsRightMouseDown => @event is { type: EventType.MouseDown, button: 1 };
        public bool IsMiddleMouseDown => @event is { type: EventType.MouseDown, button: 2 };

        public bool IsUsed => @event.type is EventType.Used;

        public bool IsKeyDown(KeyCode keyCode)
        {
            return @event is { type: EventType.KeyDown, keyCode: var k } && k == keyCode;
        }

        public bool IsKeyUp(KeyCode keyCode)
        {
            return @event is { type: EventType.KeyUp, keyCode: var k } && k == keyCode;
        }
    }
}