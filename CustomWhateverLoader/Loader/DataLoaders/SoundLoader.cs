using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Cwl.API;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Patches.Relocation;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Cwl;

internal partial class DataLoader
{
    private const string SoundExtPattern = "*.*";
    private const string SoundPathEntry = "Media/Sound/";

    internal static readonly Dictionary<string, FileInfo> CachedSounds = new(StringComparer.InvariantCultureIgnoreCase);

    internal static int LastBgmIndex { get; private set; }

    internal static void LoadAllSounds()
    {
        var dirs = PackageIterator.GetSoundFilesFromPackage();
        LoadResourcesPatch.AddHandler<SoundData>(RelocateSound);

        foreach (var dir in dirs) {
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
    }

    internal static void RebuildBGM()
    {
        var refs = Core.Instance.refs;
        if (refs.bgms.Count == 0) {
            return;
        }

        if (LastBgmIndex == 0) {
            LastBgmIndex = refs.bgms.Count;
        }

        var sm = SoundManager.current;
        var lookup = refs.bgms.ToDictionary(kv => kv.name, kv => kv);

        // TODO loc
        foreach (var soundName in CachedSounds.Keys.ToArray()) {
            if (!soundName.StartsWith("BGM/")) {
                continue;
            }

            var bgmId = soundName[4..];
            var data = sm.GetData(bgmId) as BGMData;
            if (data == null) {
                continue;
            }

            if (lookup.TryGetValue(bgmId, out var bgm)) {
                // direct replacement
                var oldLength = bgm.clip.length;
                bgm.clip = data.clip;
                CwlMod.Log<DataLoader>($"BGM replacement: {bgmId}, {oldLength}s => {bgm.clip.length}s");
            } else {
                // addon
                if (data.id <= 0) {
                    data.id = refs.bgms.Count;
                    CwlMod.Warn<DataLoader>($"assigning row based id to BGM: {data.name}, " +
                                            $"explicit id is preferred to avoid BGM lookup collision");
                }

                var duplicate = refs.bgms.FindIndex(b => b.id == data.id);
                if (duplicate != -1) {
                    CwlMod.Warn<DataLoader>($"duplicate id: {data.id}, old: {refs.bgms[duplicate].name} => new: {data.name}");
                    refs.bgms[duplicate] = data;
                } else {
                    refs.bgms.Add(data);
                    CwlMod.Log<DataLoader>($"new BGM: {data.id} {data.name}");
                }
            }
        }

        // dictBgm might include the new data, but can't depend on the race condition
        CoroutineHelper.Deferred(refs.RefreshBGM, () => Core.Instance.initialized);
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

    [CwlPostLoad]
    private static void InvalidateBGM(GameIOProcessor.GameIOContext context)
    {
        var bgms = EClass.player.knownBGMs;
        bgms.RemoveWhere(id => !Core.Instance.refs.dictBGM.ContainsKey(id));
    }

    // generate a dump with markdown
    [SuppressMessage("ReSharper", "Unity.UnknownResource")]
    private static void DumpAllBGMData()
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("Playlist Dump");

        var pls = Resources.LoadAll<Playlist>($"{SoundPathEntry}BGM/Playlist");
        foreach (var playlist in pls) {
            sb.AppendLine($"+ {playlist.name,-30}[{playlist.list.Count}]");
            foreach (var data in playlist.list.Select(item => item.data)) {
                if (data?.clip == null) {
                    continue;
                }

                sb.AppendLine($"\t+ {data.name,-40} {data.clip.length:0.##}s - {data.clip.frequency}Hz x{data.clip.channels}");
            }
        }

        sb.AppendLine("Playlist Item");
        sb.AppendLine("|id|sound name|bgm name|").AppendLine("|-|-|-|");
        foreach (var (id, bgm) in Core.Instance.refs.dictBGM) {
            sb.AppendLine($"|{id}|{bgm.name}|{bgm._name}|");
        }

        CwlMod.Log<DataLoader>(sb);
    }
}