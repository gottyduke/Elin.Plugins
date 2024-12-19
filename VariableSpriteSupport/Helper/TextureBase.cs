using System;
using System.Collections.Generic;
using UnityEngine;

namespace VSS.Helper;

public static class TextureBase
{
    internal static readonly Dictionary<string, Texture2D> Cached = [];

    public static Texture2D MakeTransparent(int width, int height)
    {
        var id = $"base_{width}x{height}";
        var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

        if (Cached.TryGetValue(id, out var cachedTexture)) {
            Graphics.CopyTexture(cachedTexture, texture);
        } else {
            var transparency = new Color32[width * height];
            Array.Fill(transparency, Color.clear);

            var cached = new Texture2D(width, height, TextureFormat.ARGB32, false);
            cached.SetPixels32(transparency);
            cached.Apply();

            Cached[id] = cached;
            Graphics.CopyTexture(cached, texture);
        }

        return texture;
    }
}