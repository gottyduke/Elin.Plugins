using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable UnusedMember.Global

namespace Cwl.API;

internal record SerializableSoundData : SerializableSoundDataV1;

internal record SerializableSoundDataV1
{
    public bool allowMultiple = true;
    public bool alwaysPlay = false;
    public SerializableBGMData bgmDataOptional = new();
    public float chance = 1f;
    public float delay = 0f;
    public bool fadeAtStart = false;
    public float fadeLength = 0f;
    public bool important = false;
    public int loop = 0;
    public float minInterval = 0f;
    public bool noSameSound = false;
    public float pitch = 1f;
    public float randomPitch = 0f;
    public float reverbMix = 1f;
    public bool skipIfPlaying = false;
    public float spatial = 0f;
    public float startAt = 0f;

    [JsonConverter(typeof(StringEnumConverter))]
    public SoundData.Type type = SoundData.Type.Default;

    public float volume = 0.5f;
    public bool volumeAsMtp = false;

    public record SerializableBGMData
    {
        public bool day = false;
        public float fadeIn = 0.1f;
        public float fadeOut = 0.5f;
        public float failDuration = 0.7f;
        public float failPitch = 0.12f;
        public bool night = false;
        public List<BGMData.Part> parts = [new()];
        public float pitchDuration = 0.01f;
    }
}