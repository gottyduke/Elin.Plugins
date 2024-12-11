using System;
using System.Collections.Generic;
using System.Linq;
using ACS.Components;
using HarmonyLib;
using UnityEngine;

namespace ACS.API;

public static class AcsClipBuilder
{
    /// <summary>
    ///     Create a single clip from sprites and cache the result.
    /// </summary>
    /// <param name="sprites">series of named sprites</param>
    /// <param name="clipName">default "loop"</param>
    public static AcsClip CreateAcsClip(this Card owner, IEnumerable<Sprite> sprites,
        string clipName = "idle",
        AcsAnimationType type = AcsAnimationType.Auto,
        float interval = 0.2f)
    {
        var frames = sprites.OrderBy(s => s.name).ToArray();
        frames.Do(f => AcsController.Cached.TryAdd(f.name, f));

        if (type == AcsAnimationType.Auto) {
            var param = clipName.Split("#");
            clipName = param[0];

            type = AcsAnimationType.Idle;
            if (Enum.TryParse<AcsAnimationType>(char.ToUpper(clipName[0]) + clipName[1..], out var parsedType)) {
                type = parsedType;
            }

            if (int.TryParse(param.ElementAtOrDefault(1), out var parsedInterval)) {
                interval = parsedInterval / 1000f;
            }
        }

        var clip = new AcsClip {
            name = clipName,
            interval = interval,
            owner = owner.id,
            type = type,
            sprites = frames,
        };

        AcsController.Clips.TryAdd(owner.id, []);
        AcsController.Clips[owner.id].Add(clip);

        AcsMod.Log($"created acs clip: {owner.id}|{clipName}|{frames.Length} frames@{interval}s");
        return clip;
    }

    /// <summary>
    ///     Create assorted clips from sprites.
    /// </summary>
    public static IEnumerable<AcsClip> CreateAcsClips(this Card owner, IEnumerable<Sprite> sprites)
    {
        Dictionary<string, List<Sprite>> temp = [];

        foreach (var sprite in sprites) {
            sprite.name = sprite.name.IsEmpty(sprite.texture.name);
            if (!sprite.name.EndsWith(".png")) {
                continue;
            }

            var parts = sprite.name.Split('_');
            if (parts.Length < 4 || parts[^3] != "acs") {
                AcsMod.Warn($"skipped frame: {sprite.name}");
                continue;
            }

            temp.TryAdd(parts[^2], []);
            temp[parts[^2]].Add(sprite);
        }

        // force enumeration
        return temp
            .Select(kv => owner.CreateAcsClip(kv.Value, kv.Key))
            .ToArray();
    }
}