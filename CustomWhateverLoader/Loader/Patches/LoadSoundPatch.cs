﻿using System.Collections;
using System.IO;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using MethodTimer;
using UnityEngine;
using UnityEngine.Networking;

namespace Cwl.Loader.Patches;

internal class LoadSoundPatch
{
    private const string Pattern = "*.wav";

    internal static IEnumerator LoadAllSounds()
    {
        var dirs = PackageIterator.GetSoundFilesFromPackage();

        foreach (var dir in dirs) {
            foreach (var file in dir.GetFiles(Pattern, SearchOption.AllDirectories)) {
                var id = file.GetFullFileNameWithoutExtension()[(dir.FullName.Length + 1)..];
                yield return LoadSound(file, id);
            }
        }
    }

    [Time]
    private static IEnumerator LoadSound(FileInfo file, string id = "")
    {
        var name = Path.GetFileNameWithoutExtension(file.FullName);

        using var clipLoader = UnityWebRequestMultimedia.GetAudioClip($"file://{file.FullName}", AudioType.WAV);
        yield return clipLoader.SendWebRequest();

        var data = ScriptableObject.CreateInstance<SoundData>();
        var metafile = $"{file.DirectoryName}/{name}.json";

        if (ConfigCereal.ReadConfig<SerializableSoundData>(metafile, out var meta) && meta is not null) {
            if (meta.type == SoundData.Type.BGM) {
                var bgm = ScriptableObject.CreateInstance<BGMData>();
                bgm.name = name;
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
            ConfigCereal.WriteConfig(meta, metafile);
            CwlMod.Log("cwl_log_sound_default_meta".Loc(id));
        }

        if (clipLoader.result != UnityWebRequest.Result.Success) {
            CwlMod.Error("cwl_error_sound_loader".Loc(id, clipLoader.error));
            yield return null;
        }

        var clip = DownloadHandlerAudioClip.GetContent(clipLoader);
        clip.name = id;

        data.clip = clip;
        data.name = id;

        SoundManager.current.dictData[id] = data;
        CwlMod.Log("cwl_log_sound_loaded".Loc(meta.type, id, clip.frequency, clip.channels, clip.length));
    }
}