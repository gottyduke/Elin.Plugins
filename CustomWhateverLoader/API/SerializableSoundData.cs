using System.Collections.Generic;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 

namespace Cwl.API;

public static class SerializableSoundDataHelper
{
    public static void WriteMetaTo(this SoundData soundData, string path)
    {
        var meta = new SerializableSoundData();
        meta.bgmDataOptional.parts.Clear();

        soundData.IntrospectCopyTo(meta);
        if (soundData is BGMData bgm) {
            // enforce type
            meta.type = SoundData.Type.BGM;
            bgm.IntrospectCopyTo(meta.bgmDataOptional);
            bgm.song.IntrospectCopyTo(meta.bgmDataOptional);
        }

        ConfigCereal.WriteConfig(meta, path);
    }
}

public record SerializableSoundData : SerializableSoundDataV1;

public record SerializableSoundDataV1
{
    [JsonConverter(typeof(StringEnumConverter))]
    public SoundData.Type type = SoundData.Type.Default;
    
    public int loop = 0;
    public float minInterval = 0f;
    
    public float chance = 1f;
    public float delay = 0f;
    public float startAt = 0f;
    public bool fadeAtStart = false;
    public float fadeLength = 0f;
    
    public float volume = 0.5f;
    public bool volumeAsMtp = false;
    
    public bool allowMultiple = true;
    public bool skipIfPlaying = false;
    public bool important = false;
    public bool alwaysPlay = false;
    public bool noSameSound = false;
    
    public float pitch = 1f;
    public float randomPitch = 0f;
    public float reverbMix = 1f;
    public float spatial = 0f;

    public SerializableBGMData bgmDataOptional = new();
    
    public record SerializableBGMData
    {
        public bool day = false;
        public bool night = false;
        
        public float fadeIn = 0.1f;
        public float fadeOut = 0.5f;
        
        public float failDuration = 0.7f;
        public float failPitch = 0.12f;
        public float pitchDuration = 0.01f;
        
        public List<BGMData.Part> parts = [new()];
    }
}
// ReSharper restore All 