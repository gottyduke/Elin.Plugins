using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cwl.Helper.Unity;

public static class SpriteCreator
{
    private static readonly Dictionary<string, Texture2D> _cached = [];

    public static Texture2D GetSolidColorTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    extension(string spritePath)
    {
        public Sprite? LoadSprite(Vector2? pivot = null,
                                  string? name = null,
                                  int resizeWidth = 0,
                                  int resizeHeight = 0)
        {
            if (spritePath.IsEmpty()) {
                return null;
            }

            string path = new(spritePath);
            if (!path.EndsWith(".png")) {
                if (SpriteReplacer.dictModItems.TryGetValue(path, out var fileById)) {
                    path = fileById;
                }

                path += ".png";
            }

            if (!File.Exists(path)) {
                var candidates = PackageIterator.GetRelocatedFilesFromPackage(Path.Combine("Texture", path))
                    .ToArray();
                if (candidates.Length == 0) {
                    return null;
                }

                path = candidates[^1].FullName;
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

    extension(Sprite sprite)
    {
        // copied from ACS
        public IEnumerable<Sprite> SliceSprite(string baseName, int width = -1, int height = -1)
        {
            if (height == -1) {
                height = (int)sprite.rect.height;
            }

            if (width == -1) {
                width = height;
            }

            if (width == 0 || height == 0) {
                yield break;
            }

            var frames = sprite.rect.width / width;
            if (frames == 0) {
                yield break;
            }

            for (var i = 0; i < frames; ++i) {
                var rect = new Rect(i * width, 0f, width, height);
                var tile = Sprite.Create(sprite.texture, rect, new(0.5f, 0.5f * (128f / height)), 100f, 0u,
                    SpriteMeshType.FullRect);
                tile.name = $"{baseName}{i:D4}";
                yield return tile;
            }
        }
    }
}