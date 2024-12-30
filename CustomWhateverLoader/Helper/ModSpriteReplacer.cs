using System.Linq;
using Cwl.Helper.Unity;
using UnityEngine;

namespace Cwl.Helper;

public class ModSpriteReplacer
{
    public static Sprite? AppendSpriteSheet(string id, int resizeWidth = 0, int resizeHeight = 0, string pattern = "@")
    {
        if (SpriteSheet.dict.TryGetValue(id, out var tile)) {
            return tile;
        }

        ref var replacers = ref SpriteReplacer.dictModItems;
        if (!replacers.TryGetValue(id, out var file) && pattern != "") {
            var matched = replacers
                .Where(kv => kv.Key.StartsWith(pattern))
                .FirstOrDefault(kv => id.StartsWith(kv.Key[pattern.Length..]));
            file ??= matched.Value;
        }

        if (file is null) {
            return null;
        }

        tile = file.LoadSprite(name: id, resizeWidth: resizeWidth, resizeHeight: resizeHeight);
        SpriteSheet.Add(tile);

        return tile;
    }
}