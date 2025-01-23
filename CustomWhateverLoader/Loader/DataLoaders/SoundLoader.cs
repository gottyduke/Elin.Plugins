using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.Patches.Relocation;
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

    internal static void LoadAllSounds()
    {
        LoadResourcesPatch.AddHandler<SoundData>(RelocateSound);

        foreach (var dir in PackageIterator.GetSoundFilesFromPackage()) {
            foreach (var file in dir.EnumerateFiles(SoundExtPattern, SearchOption.AllDirectories)) {
                try {
                    var audioType = file.Extension.ToLower() switch {
                        ".acc" => AudioType.ACC,
                        ".mp3" => AudioType.MPEG, // possible codec error
                        ".ogg" => AudioType.OGGVORBIS,
                        ".wav" => AudioType.WAV,
                        _ => AudioType.UNKNOWN,
                    };

                    if (audioType == AudioType.UNKNOWN) {
                        continue;
                    }

                    var id = file.GetFullFileNameWithoutExtension()[(dir.FullName.Length + 1)..].NormalizePath();
                    CwlMod.CurrentLoading = "cwl_log_sound_loading".Loc(id);

                    CachedSounds[id] = file;
                    CwlMod.Log<DataLoader>(CwlMod.CurrentLoading);
                } catch (Exception ex) {
                    CwlMod.Error<DataLoader>("cwl_error_sound_loader".Loc(file.Name, ex));
                    // noexcept
                }
            }
        }

        if (LastBgmIndex == 0) {
            LastBgmIndex = Core.Instance.refs.bgms.Count;
        }
    }

    private static bool RelocateSound(string path, ref Object loaded)
    {
        if (!path.StartsWith(SoundPathEntry)) {
            return false;
        }

        var id = path[SoundPathEntry.Length..];

        if (!CachedSounds.TryGetValue(id, out var file)) {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(file.FullName);
        var audioType = file.Extension.ToLower() switch {
            ".acc" => AudioType.ACC,
            ".mp3" => AudioType.MPEG, // possible codec error
            ".ogg" => AudioType.OGGVORBIS,
            ".wav" => AudioType.WAV,
            _ => AudioType.UNKNOWN,
        };

        if (audioType == AudioType.UNKNOWN) {
            return false;
        }

        using var clipLoader = UnityWebRequestMultimedia.GetAudioClip($"file://{file.FullName}", audioType);
        clipLoader.SendWebRequest();

        var sw = new Stopwatch();
        sw.Start();
        while (!clipLoader.isDone && sw.ElapsedMilliseconds < 3000) {
        }

        sw.Stop();

        if (clipLoader.result != UnityWebRequest.Result.Success) {
            CwlMod.Error<DataLoader>("cwl_error_sound_loader".Loc(id, clipLoader.error));
            return false;
        }

        var clip = DownloadHandlerAudioClip.GetContent(clipLoader);

        if (clip?.samples is not > 0) {
            CwlMod.Error<DataLoader>("cwl_error_sound_loader".Loc(id, $"Codec error/{audioType.ToString()}"));
            if (audioType == AudioType.MPEG) {
                CwlMod.Error<AudioClip>("CWL suggests using a different format than MP3, because Unity pre 2022.3.21+ had " +
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
            CwlMod.Log<DataLoader>("cwl_log_sound_default_meta".Loc(id));
        }

        data.clip = clip;
        data.name = id;

        loaded = data;
        CwlMod.Log<DataLoader>("cwl_log_sound_loaded".Loc(meta.type, id, clip.frequency, clip.channels, clip.length));

        return true;
    }
}