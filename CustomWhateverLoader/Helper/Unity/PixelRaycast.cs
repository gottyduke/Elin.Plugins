using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cwl.Helper.Unity;

// I actually hate raycasts
public static class PixelRaycast
{
    private static readonly Dictionary<string, int> _cached = [];

    public static int NearestPerceivable(this Sprite sprite, int fromX = 0, int fromY = 0,
        int directionX = 0, int directionY = 0)
    {
        var rect = sprite.rect;
        return sprite.texture.NearestPerceivable(fromX + (int)rect.xMin, fromY + (int)rect.yMin,
            directionX, directionY, (int)(rect.xMin + rect.width), (int)(rect.yMin + rect.height));
    }

    public static int NearestPerceivable(this Texture2D texture, int fromX = 0, int fromY = 0,
        int directionX = 0, int directionY = 0, int endX = 0, int endY = 0)
    {
        if (directionX == 0 && directionY == 0) {
            return -1;
        }

        var cacheKey = $"{texture.GetInstanceID()}{fromX}{fromY}{directionX}{directionY}{endX}{endY}";
        if (_cached.TryGetValue(cacheKey, out var dist)) {
            return dist;
        }

        dist = -1;

        var beginX = Math.Min(fromX, texture.width - 1);
        var beginY = Math.Min(fromY, texture.height - 1);
        endX = endX == 0 ? texture.width - 1 : Math.Min(endX, texture.width - 1);
        endY = endY == 0 ? texture.height - 1 : Math.Max(endY, texture.height - 1);

        var x = beginX;
        var y = beginY;
        var hit = false;
        while (x >= 0 && x <= endX && y >= 0 && y <= endY) {
            var rgba = texture.GetPixel(x, y);
            if (!Mathf.Approximately(rgba.a, 0f)) {
                hit = true;
                break;
            }

            x += directionX;
            y += directionY;
        }

        if (!hit) {
            _cached[cacheKey] = -1;
            return -1;
        }

        if (directionX != 0) {
            dist = Math.Abs(x - beginX);
        }

        if (directionY != 0) {
            dist = Math.Abs(y - beginY);
        }

        _cached[cacheKey] = dist;
        return dist;
    }

    public static float NearestPerceivableMulticast(this Sprite sprite, int gap, int casts = 3,
        int fromX = 0, int fromY = 0, int directionX = 0, int directionY = 0)
    {
        try {
            if (casts <= 0 || (directionX == 0 && directionY == 0)) {
                return -1;
            }

            var spread = (casts - 1) * gap / 2;
            var gapX = gap;
            var gapY = gap;

            if (directionY != 0) {
                fromX = Math.Max(0, fromX - spread);
                gapY = 0;
            }

            if (directionX != 0) {
                fromY = Math.Max(0, fromY - spread);
                gapX = 0;
            }

            var dist = new int[casts];
            for (var i = 0; i < casts; ++i) {
                dist[i] = sprite.NearestPerceivable(fromX + gapX * i, fromY + gapY * i, directionX, directionY);
            }

            dist = dist.Where(d => d != -1).ToArray();
            return dist.Length == 0 ? -1 : (float)dist.Average();
        } catch {
            // noexcept
        }

        return -1;
    }
}