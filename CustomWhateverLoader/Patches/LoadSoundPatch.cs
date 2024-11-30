using System.Collections;
using System.IO;
using Cwl.API;
using Cwl.Helper;
using UnityEngine;
using UnityEngine.Networking;

namespace Cwl.Patches;

internal class LoadSoundPatch
{
    private const string Pattern = "*.wav";

    internal static IEnumerator LoadAllSounds()
    {
        foreach (var dir in PackageFileIterator.GetSoundFilesFromPackage()) {
            foreach (var file in dir.GetFiles(Pattern, SearchOption.AllDirectories)) {
                var name = Path.GetFileNameWithoutExtension(file.FullName);
                var id = file.GetFullFileNameWithoutExtension()[(dir.FullName.Length + 1)..];

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
                    CwlMod.Log($"generated default meta for sound {id}");
                }

                if (clipLoader.result != UnityWebRequest.Result.Success) {
                    CwlMod.Log($"failed to load sound {id}\n{clipLoader.error}");
                    continue;
                }

                var clip = DownloadHandlerAudioClip.GetContent(clipLoader);
                clip.name = id;

                data.clip = clip;
                data.name = id;

                SoundManager.current.dictData[id] = data;
                CwlMod.Log($"loaded {meta.type} sound {id} {clip.frequency}Hz x{clip.channels}, {clip.length}s");
            }
        }
    }
}