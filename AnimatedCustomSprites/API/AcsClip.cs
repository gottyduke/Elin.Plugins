using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace ACS.API;

// ReSharper disable All
public sealed record AcsClip : AcsClipV1;

public record AcsClipV1
{
    public float interval = 0.2f;
    public string name = "idle";
    public string owner = "example_chara";
    internal Sprite[]? sprites = null;

    [JsonConverter(typeof(StringEnumConverter))]
    public AcsAnimationType type = AcsAnimationType.Idle;
}