using System.Linq;
using Cwl.Helper.Unity;
using UnityEngine;

namespace Cwl.Helper;

public class SpriteReplacerHelper
{
    public static Sprite? AppendSpriteSheet(string id, int resizeWidth = 0, int resizeHeight = 0, string pattern = "@")
    {
        var replacers = SpriteReplacer.dictModItems;
        if (!replacers.TryGetValue(id, out var file) && pattern != "") {
            var matched = replacers
                .Where(kv => kv.Key.StartsWith(pattern))
                .FirstOrDefault(kv => id.StartsWith(kv.Key[pattern.Length..]));
            file ??= matched.Value;
        }

        var tile = file?.LoadSprite(name: id, resizeWidth: resizeWidth, resizeHeight: resizeHeight);
        if (tile == null) {
            return null;
        }

        if (SpriteSheet.dict.TryGetValue(id, out var exist) &&
            exist.texture.width == tile.texture.width &&
            exist.texture.height == tile.texture.height) {
            return exist;
        }

        return SpriteSheet.dict[tile.name] = tile;
    }
}