using UnityEngine;

namespace ElinPad.API;

public record PadAxisState
{
    public Vector2 LastValue;
    public Vector2 Value;
}