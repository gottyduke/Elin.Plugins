using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ACS.API;

public static class AcsClipBuilder
{
    /// <summary>
    ///     Create a single clip from sprites and cache the result.
    /// </summary>
    public static AcsClip CreateAcsClip(this Card owner,
                                        Sprite[] sprites,
                                        string clipName = "",
                                        float interval = -1f,
                                        AcsAnimationType type = AcsAnimationType.Auto)
    {
        var regex = new Regex(@"_acs_(?<clipName>.*)#(?<interval>\d+)_(?<index>\d+)");
        var sprite = sprites[0];
        var param = regex.Match(sprite.name);

        if (!param.Success) {
            throw new ArgumentException("sprite could not be parsed");
        }

        var name = clipName.IsEmpty(param.Groups["clipName"].Value.ToLower());

        if (type == AcsAnimationType.Auto) {
            type = name switch {
                "idle" => AcsAnimationType.Idle,
                "combat" => AcsAnimationType.Combat,
                _ => AcsAnimationType.Condition,
            };
        }

        if (interval < 0f && !float.TryParse(param.Groups["interval"].Value, out interval)) {
            interval = 66f;
        }

        AcsMod.Log($"created clip {owner.id}/{name}/{sprite.rect.width}x{sprite.rect.height}/{sprites.Length} frames@{interval}");
        return new() {
            name = name,
            type = type,
            owner = owner.id,
            interval = interval / 1000f,
            sprites = sprites,
        };
    }

    /// <summary>
    ///     Create assorted clips from sprites.
    /// </summary>
    public static List<AcsClip> CreateAcsClips(this Card owner, IEnumerable<Sprite> sprites)
    {
        var lut = sprites.ToLookup(s => {
            var i = s.name.LastIndexOf('_');
            return i >= 0 ? s.name[..i] : s.name;
        }, s => s);

        List<AcsClip> clips = [];
        foreach (var clip in lut) {
            var name = clip.Key;
            if (!name.Contains("_acs_")) {
                continue;
            }

            var frames = clip.OrderBy(s => s.name).ToArray();
            if (frames.Length == 0) {
                continue;
            }

            clips.Add(owner.CreateAcsClip(frames));
        }

        return clips;
    }
}