using UnityEngine;

namespace ACS.Helper;

public static class PivotAdjust
{
    public static Vector2 AdjustPivot(this Texture texture)
    {
        var offset = texture.width > 128f ? 128f : 0f;
        return new(0.5f, 64f / (texture.height + offset));
    }
}