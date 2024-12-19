using UnityEngine;

namespace VSS.Helper;

public static class TextureExtender
{
    public static Texture2D ExtendBlit(this Texture2D texture, int width, int height, int tilesPerRow = 4,
        int tilesPerColumn = 4)
    {
        var extended = TextureBase.MakeTransparent(width, height);

        // blit into tiles with paddings
        var tileWidth = texture.width / tilesPerRow;
        var tileHeight = texture.height / tilesPerColumn;

        var offsetWidth = width - texture.width;
        var offsetHeight = height - texture.height;

        var xPadding = offsetWidth / tilesPerRow;
        var yPadding = offsetHeight / tilesPerColumn;

        for (var w = 0; w < tilesPerRow; ++w) {
            for (var h = 0; h < tilesPerColumn; ++h) {
                var originalX = w * tileWidth;
                var originalY = h * tileHeight;
                var extendedX = originalX + w * xPadding + xPadding / 2;
                var extendedY = originalY + h * yPadding;
                extended.SetPixels(extendedX, extendedY, tileWidth, tileHeight,
                    texture.GetPixels(originalX, originalY, tileWidth, tileHeight));
            }
        }

        extended.Apply();
        return extended;
    }
}