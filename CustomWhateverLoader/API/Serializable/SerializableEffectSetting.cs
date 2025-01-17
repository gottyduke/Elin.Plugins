using System.Collections.Generic;
using UnityEngine;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed class SerializableEffectSetting : Dictionary<string, SerializableEffectData>;

public sealed record SerializableEffectData : SerializableEffectDataV1;

public record SerializableEffectDataV1
{
    public float delay = 0.1f;
    public bool eject = true;
    public Vector2 firePos = new(0.23f, 0.04f);
    public string idEffect = "gunfire";
    public string idSound = "attack_gun";
    public int num = 1;
    public string spriteId = "ranged_gun";
}