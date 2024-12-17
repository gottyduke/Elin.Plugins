using System;
using System.Collections.Generic;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.Loader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cwl.Helper.Unity;

public static class SpriteCreator
{
    private static readonly Dictionary<string, Sprite> _cached = [];

    public static Sprite? LoadSprite(this string path, Vector2? pivot = null, string? name = null, int resizeWidth = 0,
        int resizeHeight = 0)
    {
        if (!path.EndsWith(".png")) {
            path += ".png";
        }

        pivot ??= new(0.5f, 0.5f);

        var cache = $"{path}/{pivot}/{resizeWidth}/{resizeHeight}";
        name ??= cache;

        if (_cached.TryGetValue(cache, out var sprite)) {
            return sprite;
        }

        try {
            var tex = IO.LoadPNG(path);
            if (resizeWidth != 0 && resizeHeight != 0 &&
                tex.width != resizeWidth && tex.height != resizeHeight) {
                var downscaled = tex.Downscale(resizeWidth, resizeHeight);
                Object.Destroy(tex);
                tex = downscaled;
            }

            sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height),
                pivot.Value, 100f, 0u, SpriteMeshType.FullRect);

            sprite.name = tex.name = name;
            _cached[cache] = sprite;
        } catch (Exception ex) {
            CwlMod.Warn("cwl_warn_sprite_creator".Loc(path.ShortPath(), ex));
            return null;
            // noexcept
        }

        return sprite;
    }
}