using System.Collections.Generic;
using System.Linq;
using Cwl.API.Processors;
using UnityEngine;

namespace Cwl.API.Custom;

public partial class CustomPlaylist(string name, int[] merge, int[] remove, bool shuffle = true)
{
    private static readonly List<CustomPlaylist> _loaded = [];
    private static readonly Dictionary<string, CustomPlaylist> _cached = [];
    private static readonly Dictionary<string, Playlist> _merged = [];
    private static ILookup<string, CustomPlaylist>? _lut;
    private static bool _dirty;

    public static ILookup<string, CustomPlaylist> Lut
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

        var zoneName = zone.GetType().Name;
        var baseName = GetBasePlaylistName(mold.name, zoneName);

        var plName = "CWL_Merged_Global_";
        if (baseName != "") {
            plName += $"{baseName}_";
        }

        plName += zoneName;

        if (_merged.TryGetValue(plName, out var playlist)) {
            return playlist;
        }

        playlist = mold.Instantiate();

        var list = playlist.ToInts();
        playlist.list.Clear();

        string[] orders = ["Global", baseName, zoneName];
        var shuffle = false;
        foreach (var order in orders) {
            var lists = MergeOverrides(Lut[order], zoneName);

            list.RemoveAll(lists.ListRemove.Contains);
            list.AddRange(lists.ListMerge);

            shuffle = shuffle || lists.Shuffle;
        }

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

        playlist.name = plName;
        playlist.shuffle = shuffle;

        return _merged[plName] = playlist;
    }

    private static CustomPlaylist MergeOverrides(IEnumerable<CustomPlaylist> overrides, string zoneName)
    {
        var lists = overrides.ToArray();

        List<string> names = [];
        foreach (var pl in lists) {
            names.Add(pl.Name);
        }

        names.Add(zoneName);

        var cacheName = $"{string.Join("_", names)}/{lists.Length}";
        if (_cached.TryGetValue(cacheName, out var playlist)) {
            return playlist;
        }

        HashSet<int> merges = [];
        HashSet<int> remove = [];

        var shuffle = false;
        foreach (var pl in lists) {
            merges.UnionWith(pl.ListMerge);
            remove.UnionWith(pl.ListRemove);

            shuffle |= pl.Shuffle;
        }

        var dict = Core.Instance.refs.dictBGM;
        merges.RemoveWhere(id => !dict.ContainsKey(id));
        remove.RemoveWhere(id => !dict.ContainsKey(id));

        playlist = new(string.Join("_", names.Distinct()), merges.ToArray(), remove.ToArray(), shuffle);
        return _cached[cacheName] = playlist;
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