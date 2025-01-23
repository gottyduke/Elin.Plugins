using System.Collections.Generic;
using System.Linq;
using Cwl.API.Processors;
using UnityEngine;

namespace Cwl.API.Custom;

public partial class CustomPlaylist(string name, int[] merge, int[] remove, bool shuffle = true)
{
    internal static readonly List<CustomPlaylist> _loaded = [];

    private static readonly Dictionary<string, CustomPlaylist> _mergeCache = [];
    private static ILookup<string, CustomPlaylist>? _lut;
    private static bool _dirty;

    internal static ILookup<string, CustomPlaylist> Lut
    {
        get
        {
            if (_lut is null || _dirty) {
                _lut = _loaded.ToLookup(p => p.Name, p => p);
            }

            return _lut;
        }
    }

    public string Name => name;
    public int[] ListMerge => merge;
    public int[] ListRemove => remove;
    public bool Shuffle => shuffle;

    public static Playlist GeneratePlaylistForZone(Playlist? mold, Zone zone)
    {
        mold ??= zone.map?.plDay;
        if (mold == null) {
            return ScriptableObject.CreateInstance<Playlist>();
        }

        var playlist = mold.Instantiate();

        var zoneName = zone.GetType().Name;
        var baseName = GetBasePlaylistName(playlist.name, zoneName);
        var @override = MergeOverrides([
            ..Lut["Global"],
            ..Lut[baseName],
            ..Lut[zoneName],
        ]);

        var list = playlist.ToInts();
        playlist.list.Clear();

        list.AddRange(@override.ListMerge);
        list.RemoveAll(@override.ListRemove.Contains);

        if (list.Count == 0) {
            list.Add(41);
        }

        var dict = Core.Instance.refs.dictBGM;

        foreach (var id in list) {
            if (!dict.TryGetValue(id, out var bgm) || bgm?.clip == null) {
                continue;
            }

            playlist.list.Add(new() { data = bgm, isLoading = false });
        }

        playlist.shuffle = @override.Shuffle;
        playlist.name = @override.Name;

        return playlist;
    }

    public static CustomPlaylist MergeOverrides(CustomPlaylist[] overrides)
    {
        List<string> names = ["CWL_Merged"];
        foreach (var pl in overrides) {
            names.Add(pl.Name);
        }

        var cacheName = $"{string.Join("_", names)}/{overrides.Length}";
        if (_mergeCache.TryGetValue(cacheName, out var playlist)) {
            return playlist;
        }
        
        HashSet<int> merges = [];
        HashSet<int> remove = [];

        var shuffle = false;
        foreach (var pl in overrides) {
            merges.UnionWith(pl.ListMerge);
            remove.UnionWith(pl.ListRemove);

            shuffle |= pl.Shuffle;
        }

        var dict = Core.Instance.refs.dictBGM;
        merges.RemoveWhere(id => !dict.ContainsKey(id));
        remove.RemoveWhere(id => !dict.ContainsKey(id));

        playlist = new(string.Join("_", names.Distinct()), merges.ToArray(), remove.ToArray(), shuffle);
        return _mergeCache[cacheName] = playlist;
    }

    private static string GetBasePlaylistName(string fullName, string zoneName)
    {
        return fullName.Replace("Playlist_", "").Replace(zoneName, "").TrimEnd('_');
    }

    [CwlPostLoad]
    private static void InvalidateBGM(GameIOProcessor.GameIOContext context)
    {
        var bgms = EClass.player.knownBGMs;
        bgms.RemoveWhere(id => !Core.Instance.refs.dictBGM.ContainsKey(id));
    }
}