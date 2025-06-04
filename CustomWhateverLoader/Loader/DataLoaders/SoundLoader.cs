using System;
using System.Collections.Generic;
using System.IO;
using Cwl.API;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Runtime;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using MethodTimer;
using ReflexCLI.Attributes;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Cwl;

internal partial class DataLoader
{
    internal const string SoundPathEntry = "Media/Sound/";
    private const string SoundExtPattern = "*.*";

    internal static readonly Dictionary<string, FileInfo> CachedSounds = new(StringComparer.InvariantCultureIgnoreCase);

    internal static int LastBgmIndex { get; private set; }

    [Time]
    [ConsoleCommand("load_sound")]
    internal static void LoadAllSounds()
    {
        if (LastBgmIndex == 0) {
            LastBgmIndex = Core.Instance.refs.bgms.Count;
        } else {
            // hot reload
            ClearSoundCache();
        }

        foreach (var dir in PackageIterator.GetSoundFilesFromPackage()) {
            foreach (var file in dir.EnumerateFiles(SoundExtPattern, SearchOption.AllDirectories)) {
                try {
                    var audioType = GetAudioType(file.Extension);
                    if (audioType == AudioType.UNKNOWN) {
                        continue;
                    }

                    var id = file.GetFullFileNameWithoutExtension()[(dir.FullName.Length + 1)..].NormalizePath();
                    CwlMod.CurrentLoading = "cwl_log_sound_loading".Loc(id);

                    CachedSounds[id] = file;
                    CwlMod.Log<DataLoader>(CwlMod.CurrentLoading);
                } catch (Exception ex) {
                    CwlMod.WarnWithPopup<DataLoader>("cwl_error_sound_loader".Loc(file.Name, ex));
                    // noexcept
                }
            }
        }

        foreach (var (id, file) in CachedSounds) {
            var metafile = $"{file.DirectoryName}/{Path.GetFileNameWithoutExtension(file.Name)}.json";
            if (File.Exists(metafile)) {
                continue;
            }

            // generate meta for first time sounds
            _ = SoundManager.current.GetData(id);
        }
    }

    [ConsoleCommand("clear_sound_cache")]
    private static void ClearSoundCache()
    {
        CachedSounds.Clear();
        SoundManager.current.dictData.Clear();

        if (LastBgmIndex == 0) {
            return;
        }

        var count = Core.Instance.refs.bgms.Count - LastBgmIndex;
        Core.Instance.refs.bgms.RemoveRange(LastBgmIndex, count);
        CwlMod.Log<DataLoader>($"removed {count} cached bgm entries");
    }

    internal static bool RelocateSound(string path, ref Object? loaded)
    {
        if (!path.StartsWith(SoundPathEntry, StringComparison.InvariantCultureIgnoreCase)) {
            return false;
        }

        var id = path[SoundPathEntry.Length..];

        if (!CachedSounds.TryGetValue(id, out var file)) {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(file.FullName);
        var audioType = GetAudioType(file.Extension);
        if (audioType == AudioType.UNKNOWN) {
            return false;
        }

        var streaming = id.StartsWith("BGM/");
        using var clipLoader = AudioClipStream.GetAudioClip($"file://{file.FullName}", audioType, false, streaming);
        clipLoader.SendWebRequest();

        while (!clipLoader.isDone) {
        }

        if (clipLoader.result != UnityWebRequest.Result.Success) {
            CwlMod.WarnWithPopup<DataLoader>("cwl_error_sound_loader".Loc(id, clipLoader.error));
            return false;
        }

        var clip = DownloadHandlerAudioClip.GetContent(clipLoader);
        if (clip?.samples is not > 0) {
            CwlMod.WarnWithPopup<DataLoader>("cwl_error_sound_loader".Loc(id, $"Codec error/{audioType.ToString()}"));
            if (audioType == AudioType.MPEG) {
                CwlMod.WarnWithPopup<AudioClip>(
                    "CWL suggests using a different format than MP3, because Unity pre 2022.3.21+ had " +
                    "problems decoding long MP3 audio files.");
            }

            return false;
        }

        clip.name = id;

        var data = ScriptableObject.CreateInstance<SoundData>();
        var metafile = $"{file.DirectoryName}/{name}.json";
        if (ConfigCereal.ReadConfig<SerializableSoundData>(metafile, out var meta) && meta is not null) {
            if (meta.type == SoundData.Type.BGM) {
                var bgm = ScriptableObject.CreateInstance<BGMData>();
                bgm._name = name.Capitalize().ToString();
                bgm.song = new();

                meta.bgmDataOptional.IntrospectCopyTo(bgm);
                meta.bgmDataOptional.parts.RemoveAt(0);
                meta.bgmDataOptional.IntrospectCopyTo(bgm.song);

                Object.Destroy(data);
                data = bgm;
            }

            meta.IntrospectCopyTo(data);
        } else {
            meta = new();
            if (id.StartsWith("BGM/")) {
                var bgm = ScriptableObject.CreateInstance<BGMData>();
                Object.Destroy(data);
                data = bgm;
                data.type = meta.type = SoundData.Type.BGM;
            }

            ConfigCereal.WriteConfig(meta, metafile);
            CwlMod.Debug<DataLoader>("cwl_log_sound_default_meta".Loc(id));
        }

        data.clip = clip;
        data.name = id;

        loaded = data;
        CwlMod.Debug<DataLoader>("cwl_log_sound_loaded".Loc(meta.type, id, clip.frequency, clip.channels, clip.length));

        return true;
    }

    private static AudioType GetAudioType(string extension)
    {
        return extension.ToLower().Trim() switch {
            ".acc" => AudioType.ACC,
            ".mp3" => AudioType.MPEG, // possible codec error
            ".ogg" => AudioType.OGGVORBIS,
            ".wav" => AudioType.WAV,
            _ => AudioType.UNKNOWN,
        };
    }
}