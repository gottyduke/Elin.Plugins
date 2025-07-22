using System.Collections.Generic;
using UnityEngine;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed class SerializableEffectSetting : Dictionary<string, SerializableEffectData>;

public sealed record SerializableEffectData : SerializazbleEffectDataV2;

public record SerializazbleEffectDataV2 : SerializableEffectDataV1
{
    public string caneColor = "";
    public bool caneColorBlend = false;
    public string idSprite = "ranged_gun";
}

// use lowercase naming for introspect copy
public record SerializableEffectDataV1
{
    public float delay = 0.1f;
    public bool eject = true;
    public Vector2 firePos = new(0.23f, 0.04f);
    public string idEffect = "gunfire";
    public string idSound = "attack_gun";
    public int num = 1;
    public string spriteId = "";
}