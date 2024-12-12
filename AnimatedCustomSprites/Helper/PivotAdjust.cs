using UnityEngine;

namespace ACS.Helper;

public static class PivotAdjust
{
    public static float AdjustPivot(this Texture texture, float? widthOverride = null)
    {
        widthOverride ??= texture.width;
        var offset = widthOverride > 128f ? 128f : 0f;
        return texture.height / (texture.height + offset) / 2f;
    }
}