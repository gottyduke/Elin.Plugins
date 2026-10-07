using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextureExpand.Rules;
using UnityEngine;

namespace TextureExpand.Textures;

internal static class TileCompositor
{
    private static readonly Dictionary<string, Pixels> _images = [];

    private static Dictionary<string, string>? _replacements;

    internal static void Clear()
    {
        _images.Clear();
        _replacements = null;
    }

    internal static void Blit(List<RuleLayer>? layers,
                              Texture2D source,
                              CustomRenderData data,
                              int tile,
                              int dstTile,
                              int cellWidth,
                              int cellHeight)
    {
        if (dstTile < 0) {
            TextureExpand.LogError($"Not enough space for tile: {tile}");
            return;
        }

        var sheet = data.Texture!;
        var x = dstTile % 100 * cellWidth;
        var y = sheet.height - dstTile / 100 * cellHeight - cellHeight;

        Color[] pixels;
        if (layers is { Count: > 0 }) {
            pixels = Compose(layers, source, tile, cellWidth, cellHeight);
        } else if (TryGetReplacement($"objC_{tile}.png", out var path)) {
            pixels = IO.LoadPNG(path).GetPixels();
        } else {
            pixels = GetTile(source, tile, cellWidth, cellHeight);
        }

        if (pixels.Length != cellWidth * cellHeight) {
            TextureExpand.LogError(
                $"Invalid size: png required pixelNum={cellWidth * cellHeight}({cellWidth}x{cellHeight}), but {pixels.Length}");
            return;
        }

        sheet.SetPixels(x, y, cellWidth, cellHeight, pixels);
    }

    private static Color[] Compose(List<RuleLayer> layers, Texture2D source, int tile, int cellWidth, int cellHeight)
    {
        Color[]? canvas = null;
        var maxWidth = 0;
        var maxHeight = 0;
        var canvasWidth = 0;
        var canvasHeight = 0;

        foreach (var layer in layers.OrderBy(l => l.Index)) {
            var offset = layer.Offset;
            var image = Load(layer.Path, tile);

            int width;
            int height;
            Color[] pixels;
            if (image is not null) {
                width = image.Width;
                height = image.Height;
                maxWidth = Mathf.Max(maxWidth, width + offset.x);
                maxHeight = Mathf.Max(maxHeight, height + offset.y);
                pixels = [..image.Data];
            } else {
                if (!int.TryParse(layer.Path, out var vanilla)) {
                    vanilla = tile;
                }

                width = cellWidth;
                height = cellHeight;
                maxWidth = Mathf.Max(maxWidth, cellWidth);
                maxHeight = Mathf.Max(maxHeight, cellHeight);
                pixels = GetTile(source, vanilla, cellWidth, cellHeight);
            }

            if (canvas is null) {
                if (pixels.Length == maxWidth * maxHeight) {
                    canvas = pixels;
                } else {
                    canvas = new Color[maxWidth * maxHeight];
                    for (var py = 0; py < height; py++) {
                        for (var px = 0; px < width; px++) {
                            var cx = px + offset.x;
                            var cy = py + offset.y;
                            if (cx >= 0 && cx < maxWidth && cy >= 0 && cy < maxHeight) {
                                canvas[cx + cy * maxWidth] = pixels[px + py * width];
                            }
                        }
                    }
                }

                canvasWidth = maxWidth;
                canvasHeight = maxHeight;
                continue;
            }

            if (canvas.Length < maxWidth * maxHeight) {
                var grown = new Color[maxWidth * maxHeight];
                for (var cy = 0; cy < maxHeight; cy++) {
                    for (var cx = 0; cx < maxWidth; cx++) {
                        grown[cx + cy * maxWidth] = cx < canvasWidth && cy < canvasHeight
                            ? canvas[cx + cy * canvasWidth]
                            : Color.clear;
                    }
                }

                canvas = grown;
                canvasWidth = maxWidth;
                canvasHeight = maxHeight;
            }

            for (var py = 0; py < height; py++) {
                for (var px = 0; px < width; px++) {
                    var cx = px + offset.x;
                    var cy = py + offset.y;
                    if (cx < 0 || cx >= canvasWidth || cy < 0 || cy >= canvasHeight) {
                        continue;
                    }

                    var over = pixels[px + py * width];
                    var index = cx + cy * canvasWidth;
                    var under = canvas[index];
                    var blended = Color.Lerp(under, over, over.a);
                    blended.a = under.a + over.a * (1f - under.a);
                    canvas[index] = blended;
                }
            }
        }

        return canvas!;
    }

    private static Pixels? Load(string path, int tile)
    {
        if (_images.TryGetValue(path, out var image)) {
            return image;
        }

        var fallback = $"objC_{tile}.png";
        if (_images.TryGetValue(fallback, out image)) {
            return image;
        }

        var texture = IO.LoadPNG(path);
        if (texture) {
            return _images[path] = new(texture);
        }

        if (TryGetReplacement(fallback, out var replacement)) {
            return _images[fallback] = new(IO.LoadPNG(replacement));
        }

        return null;
    }

    private static Color[] GetTile(Texture2D source, int tile, int cellWidth, int cellHeight)
    {
        var x = tile % 100 * cellWidth;
        var y = source.height - tile / 100 * cellHeight - cellHeight;
        return source.GetPixels(x, y, cellWidth, cellHeight);
    }

    private static bool TryGetReplacement(string fileName, out string path)
    {
        if (_replacements is null) {
            _replacements = [];

            var user = new DirectoryInfo(Path.Combine(CorePath.user, "Texture Replace"));
            if (user.Exists) {
                foreach (var file in user.GetFiles()) {
                    _replacements.TryAdd(file.Name, file.FullName);
                }
            }

            foreach (var file in EClass.core.mods.replaceFiles) {
                _replacements.TryAdd(file.Name, file.FullName);
            }
        }

        return _replacements.TryGetValue(fileName, out path);
    }

    private sealed class Pixels
    {
        internal Pixels(Texture2D texture)
        {
            Width = texture.width;
            Height = texture.height;
            Data = texture.GetPixels();
        }

        internal int Width { get; }
        internal int Height { get; }
        internal Color[] Data { get; }
    }
}