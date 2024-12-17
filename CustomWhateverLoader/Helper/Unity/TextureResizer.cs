using UnityEngine;

namespace Cwl.Helper.Unity;

public static class TextureResizer
{
    public static Texture2D Downscale(this Texture2D texture, int width, int height, Material? mat = null)
    {
        var renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);

        if (mat == null) {
            Graphics.Blit(texture, renderTexture);
        } else {
            Graphics.Blit(texture, renderTexture, mat);
        }

        var downscaled = new Texture2D(width, height, TextureFormat.ARGB32, false);
        downscaled.ReadPixels(new(0, 0, width, height), 0, 0);
        downscaled.Apply();

        return downscaled;
    }
}