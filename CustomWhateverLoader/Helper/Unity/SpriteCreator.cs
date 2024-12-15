using System;
using System.Collections.Generic;
using Cwl.Helper.String;
using Cwl.LangMod;
using UnityEngine;

namespace Cwl.Helper.Unity;

public static class SpriteCreator
{
    private static readonly Dictionary<string, Sprite> _cached = [];

    public static Sprite? LoadSprite(this string path, Vector2? pivot = null)
    {
        pivot ??= new(0.5f, 0.5f);
        var cache = $"{path}/{pivot}";

        if (_cached.TryGetValue(cache, out var sprite)) {
            return sprite;
        }

        try {
            var tex = IO.LoadPNG(path);
            sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height),
                pivot.Value, 100f, 0u, SpriteMeshType.FullRect);
            _cached[cache] = sprite;
        } catch (Exception ex) {
            CwlMod.Warn("cwl_warn_sprite_creator".Loc(path.ShortPath(), ex));
            return null;
            // noexcept
        }

        return sprite;
    }
}