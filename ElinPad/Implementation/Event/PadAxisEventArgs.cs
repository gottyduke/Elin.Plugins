using ElinPad.API.Event;
using UnityEngine;

namespace ElinPad.Implementation.Event;

public class PadAxisEventArgs(PadAxisEventType axis, Vector2 value, Vector2 delta) :
    PadEventArgs
{
    public PadAxisEventType Axis => axis;
    public Vector2 Value => value;
    public Vector2 Delta => delta;
}