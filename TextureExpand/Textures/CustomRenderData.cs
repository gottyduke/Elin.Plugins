using System.Collections.Generic;
using UnityEngine;

namespace TextureExpand.Textures;

internal sealed class CustomRenderData
{
    internal Texture2D? Texture;

    internal bool UseCache;

    internal CustomRenderData(RenderData renderData,
                              string textureName,
                              int width,
                              int height,
                              Material material,
                              SortedSet<int> remainTiles)
    {
        RenderData = renderData;
        TextureName = textureName;
        TextureWidth = width;
        TextureHeight = height;
        Material = material;
        RemainTiles = remainTiles;
    }

    internal RenderData RenderData { get; }
    internal string TextureName { get; }
    internal int TextureWidth { get; }
    internal int TextureHeight { get; }
    internal Material Material { get; }

    internal SortedSet<int> RemainTiles { get; }
}