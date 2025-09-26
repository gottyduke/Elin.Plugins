using System.Collections.Generic;
using UnityEngine;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed class SerializableEffectSetting : Dictionary<string, SerializableEffectData>;

public sealed record SerializableEffectData : SerializableEffectDataV3;

public record SerializableEffectDataV3 : SerializableEffectDataV2
{
    public string idSoundEject = "bullet_drop";
    public bool forceLaser = false;
    public bool forceRail = false;
    public bool fireFromMuzzle = false;
}

public record SerializableEffectDataV2 : SerializableEffectDataV1
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
    /*  gun         attack_gun
     *  gun_assult  attack_gun_assault
     *  bow         attack_bow
     *  cane        attack_cane
     *  rail        attack_gun_rail
     *  laser       attack_gun_laser
     *  mani        attack_gun_shotgun
     *  windbow     attack_windbow
     */
    public string idSound = "attack_gun";
    public int num = 1;
    // legacy leftover
    public string spriteId = "";
}