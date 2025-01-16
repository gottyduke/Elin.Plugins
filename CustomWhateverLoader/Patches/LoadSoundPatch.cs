using System.Collections;
using System.IO;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;
using UnityEngine;
using UnityEngine.Networking;

namespace Cwl.Patches;

internal class LoadSoundPatch
{
    private const string Pattern = "*.*";

    internal static IEnumerator LoadAllSounds()
    {
        var dirs = PackageIterator.GetSoundFilesFromPackage();

        foreach (var dir in dirs) {
            foreach (var file in dir.EnumerateFiles(Pattern, SearchOption.AllDirectories)) {
                var id = file.GetFullFileNameWithoutExtension()[(dir.FullName.Length + 1)..].NormalizePath();
                CwlMod.CurrentLoading = $"[CWL] loading sound {id}";

                yield return LoadSound(file, id);
            }
        }
    }

    [Time]
    private static IEnumerator LoadSound(FileInfo file, string id = "")
    {
        var name = Path.GetFileNameWithoutExtension(file.FullName);

        var audioType = file.Extension.ToLower() switch {
            ".acc" => AudioType.ACC,
            ".mp3" => AudioType.MPEG,
            ".ogg" => AudioType.OGGVORBIS,
            ".wav" => AudioType.WAV,
            _ => AudioType.UNKNOWN,
        };
        if (audioType == AudioType.UNKNOWN) {
            yield break;
        }

        using var clipLoader = UnityWebRequestMultimedia.GetAudioClip($"file://{file.FullName}", audioType);
        yield return clipLoader.SendWebRequest();

        var data = ScriptableObject.CreateInstance<SoundData>();
        var metafile = $"{file.DirectoryName}/{name}.json";

        if (ConfigCereal.ReadConfig<SerializableSoundData>(metafile, out var meta) && meta is not null) {
            if (meta.type == SoundData.Type.BGM) {
                var bgm = ScriptableObject.CreateInstance<BGMData>();
                bgm.name = name;
                bgm.song = new();

                meta.bgmDataOptional.IntrospectCopyTo(bgm);
                meta.bgmDataOptional.parts.Clear();
                meta.bgmDataOptional.IntrospectCopyTo(bgm.song);

                Object.Destroy(data);
                data = bgm;
            }

            meta.IntrospectCopyTo(data);
        } else {
            meta = new();
            ConfigCereal.WriteConfig(meta, metafile);
            CwlMod.Log<SoundData>("cwl_log_sound_default_meta".Loc(id));
        }

        if (clipLoader.result != UnityWebRequest.Result.Success) {
            CwlMod.Error<SoundData>("cwl_error_sound_loader".Loc(id, clipLoader.error));
            yield return null;
        }

        var clip = DownloadHandlerAudioClip.GetContent(clipLoader);
        clip.name = id;

        data.clip = clip;
        data.name = id;

        SoundManager.current.dictData[id] = data;
        CwlMod.Log<SoundData>("cwl_log_sound_loaded".Loc(meta.type, id, clip.frequency, clip.channels, clip.length));
    }
}