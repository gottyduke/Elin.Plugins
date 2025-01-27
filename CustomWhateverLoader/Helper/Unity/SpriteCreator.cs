using System;
using System.Collections.Generic;
using Cwl.Helper.String;
using Cwl.LangMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cwl.Helper.Unity;

public static class SpriteCreator
{
    private static readonly Dictionary<string, Texture2D> _cached = [];

    public static Sprite? LoadSprite(this string path, Vector2? pivot = null, string? name = null, int resizeWidth = 0,
        int resizeHeight = 0)
    {
        if (!path.EndsWith(".png")) {
            path += ".png";
        }

        pivot ??= new(0.5f, 0.5f);

        var cache = $"{path}/{pivot}/{resizeWidth}/{resizeHeight}";
        name ??= cache;

        try {
            if (!_cached.TryGetValue(cache, out var tex) ||
                !CwlConfig.CacheSprites) {
                tex = IO.LoadPNG(path);
                if (tex == null) {
                    return null;
                }

                if (resizeWidth != 0 && resizeHeight != 0 &&
                    tex.width != resizeWidth && tex.height != resizeHeight) {
                    var downscaled = tex.Downscale(resizeWidth, resizeHeight);
                    Object.Destroy(tex);
                    tex = downscaled;
                    tex.name = cache;
                }
            }

            _cached[cache] = tex;
        } catch (Exception ex) {
            CwlMod.WarnWithPopup<Sprite>("cwl_warn_sprite_creator".Loc(path.ShortPath(), ex.Message), ex);
            return null;
            // noexcept
        }

        var sprite = Sprite.Create(_cached[cache], new(0, 0, _cached[cache].width, _cached[cache].height),
            pivot.Value, 100f, 0u, SpriteMeshType.FullRect);

        sprite.name = name;
        return sprite;
    }
}