using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
using ReflexCLI.Attributes;

namespace Cwl.API.Custom;

public partial class CustomPlaylist
{
    public static bool AddOrReplaceBGM(string bgmName)
    {
        var bgms = Core.Instance.refs.bgms;
        var dict = Core.Instance.refs.dictBGM;
        var sm = SoundManager.current;

        var bgmId = bgmName[4..];
        var data = sm.GetData(bgmId) as BGMData;
        if (data == null) {
            return false;
        }

        // unassigned id
        if (data.id <= 0) {
            data.id = bgms.Count + 1;
            CwlMod.Warn<CustomPlaylist>("cwl_warn_bgm_id_collision".Loc(data.name, data.id));
        }

        if (dict.TryGetValue(data.id, out var bgm)) {
            // direct replacement
            bgm.clip = data.clip;
            CwlMod.Log<CustomPlaylist>("cwl_log_bgm_clip_replace".Loc(data.id, bgm.name, data.name));
        } else {
            // addon
            bgms.Add(data);
            dict[data.id] = data;
            CwlMod.Log<CustomPlaylist>("cwl_log_bgm_added".Loc(data.id, data.name));
        }

        return true;
    }

    [ConsoleCommand("reimport")]
    internal static void RebuildBGM()
    {
        if (DataLoader.LastBgmIndex == 0) {
            // ???
            return;
        }

        SafeRebuild();

        foreach (var soundName in DataLoader.CachedSounds.Keys.ToArray()) {
            try {
                if (!soundName.StartsWith("BGM/")) {
                    continue;
                }

                AddOrReplaceBGM(soundName);
            } catch (Exception ex) {
                CwlMod.Warn<CustomPlaylist>("cwl_error_sound_loader".Loc(soundName, ex));
                // noexcept
            }
        }
    }

    [ConsoleCommand("rebuild")]
    internal static void BuildPlaylists()
    {
        _loaded.Clear();
        _cached.Clear();
        _lut = null;

        foreach (var dir in PackageIterator.GetSoundFilesFromPackage()) {
            try {
                var playlistDir = new DirectoryInfo(Path.Combine(dir.FullName, "BGM/Playlist"));
                if (!playlistDir.Exists) {
                    continue;
                }

                var playlists = playlistDir.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly);
                foreach (var playlist in playlists) {
                    if (!ConfigCereal.ReadConfig<SerializablePlaylist>(playlist.FullName, out var data) || data is null) {
                        continue;
                    }

                    var merge = MapToId(data.List);
                    var remove = MapToId(data.Remove);
                    if (merge.Length == 0 && remove.Length == 0) {
                        CwlMod.Warn<CustomPlaylist>("cwl_warn_playlist_empty".Loc(playlist.ShortPath()));
                        continue;
                    }

                    var name = Path.GetFileNameWithoutExtension(playlist.Name);
                    _loaded.Add(new(name, merge, remove, data.Shuffle));

                    var provider = $"{dir.Parent!.Name}/{playlist.Name}";
                    CwlMod.Log<CustomPlaylist>("cwl_log_playlist_added".Loc(provider, merge.Length, remove.Length));
                }
            } catch (Exception ex) {
                CwlMod.Warn<CustomPlaylist>("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }

        _dirty = true;
        
        // hot reload
        if (EClass.core.IsGameStarted) {
            EClass._zone.RefreshBGM();
        }
    }

    private static int[] MapToId(IEnumerable<string> names)
    {
        HashSet<int> map = [];
        foreach (var name in names) {
            if (name == "**") {
                map.UnionWith(Core.Instance.refs.dictBGM.Keys);
                continue;
            }
            
            if (name.Contains("/*")) {
                var pattern = name[..name.LastIndexOf('/')];
                var match = Core.Instance.refs.dictBGM
                    .Where(kv => kv.Value.name.StartsWith($"BGM/{pattern}"))
                    .Select(kv => kv.Key);
                map.UnionWith(match);
                continue;
            }
            
            var id = ReverseId.BGM(name);
            if (id > 0) {
                map.Add(id);
            }
        }

        return map.ToArray();
    }

    [SwallowExceptions]
    private static void SafeRebuild()
    {
        Core.Instance.refs.RefreshBGM();
    }
}