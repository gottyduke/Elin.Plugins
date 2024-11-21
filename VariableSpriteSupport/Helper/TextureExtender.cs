using System;
using System.Collections.Generic;
using UnityEngine;

namespace VSS.Helper;

internal static class TextureExtender
{
    private static readonly Dictionary<string, Texture2D> _cached = [];

    internal static Texture2D MakeBaseTexture(int width, int height)
    {
        var id = $"base_{width}x{height}";
        var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

        if (_cached.TryGetValue(id, out var cachedTexture)) {
            Graphics.CopyTexture(cachedTexture, texture);
        } else {
            var cached = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var transparent = new Color32[width * height];
            Array.Fill(transparent, Color.clear);
            cached.SetPixels32(transparent);
            cached.Apply();
            _cached[id] = cached;

            Graphics.CopyTexture(cached, texture);
        }

        return texture;
    }

    internal static Texture2D ExtendBlit(this Texture2D texture, int width, int height, int tilesPerRow = 4,
        int tilesPerColumn = 4)
    {
        var extended = MakeBaseTexture(width, height);

        // blit into 16 tiles with paddings
        var tileWidth = texture.width / 4;
        var tileHeight = texture.height / 4;

        var offsetWidth = width - texture.width;
        var offsetHeight = height - texture.height;

        var xPadding = Mathf.RoundToInt(offsetWidth / (tilesPerRow + 1f));
        var yPadding = offsetHeight / tilesPerColumn;

        for (var w = 0; w < 4; ++w) {
            for (var h = 0; h < 4; ++h) {
                var originalX = w * tileWidth;
                var originalY = h * tileHeight;
                var extendedX = originalX + (w + 1) * xPadding;
                var extendedY = originalY + h * yPadding;
                extended.SetPixels(extendedX, extendedY, tileWidth, tileHeight,
                    texture.GetPixels(originalX, originalY, tileWidth, tileHeight));
            }
        }

        extended.Apply();
        return extended;
    }
}