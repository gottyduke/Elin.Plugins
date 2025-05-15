using System;
using System.Collections.Generic;
using Cwl.Helper.Unity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cwl;

internal partial class DataLoader
{
    internal const string EffectPathEntry = "Media/Effect";

    internal static bool RelocateEffect(string path, ref Object? loaded)
    {
        if (!path.StartsWith(EffectPathEntry, StringComparison.InvariantCultureIgnoreCase)) {
            return false;
        }

        var effectSheet = path.Split('/').LastItem().LoadSprite();
        if (effectSheet == null) {
            return false;
        }

        return false;
    }

    // copied from ACS
    private static IEnumerable<Sprite> SliceSheet(Sprite sheet, int begin, int end, string baseName)
    {
        var frames = end - begin + 1;
        var width = sheet.rect.width / frames;
        var height = sheet.rect.height;

        for (var i = 0; i < frames; ++i) {
            Rect rect = new(i * width, 0f, width, height);
            var sprite = Sprite.Create(sheet.texture, rect, new(0.5f, 0.5f * (128f / height)), 100f, 0u, SpriteMeshType.FullRect);
            sprite.name = $"{baseName}{i:D4}";
            yield return sprite;
        }
    }
}