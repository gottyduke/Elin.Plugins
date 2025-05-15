using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
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
        try {
            mold ??= zone.map?.plDay;
            if (mold == null) {
                return ScriptableObject.CreateInstance<Playlist>();
            }

            var zoneName = zone.GetType().Name;
            var baseName = GetBasePlaylistName(mold.name);

            var plName = "CWL_Merged_Global_";
            if (baseName != "") {
                plName += $"{baseName}_";
            }

            plName += zoneName;

            var cacheName = $"{plName}_{mold.UniqueString()}";
            if (_merged.TryGetValue(cacheName, out var playlist)) {
                return playlist;
            }

            var list = mold.ToInts();
            playlist = mold.Instantiate();
            playlist.list.Clear();

            string[] orders = ["Global", baseName, zoneName];
            var shuffle = false;
            foreach (var order in orders) {
                var lists = MergeOverrides(Lut[order], zoneName);

                list.RemoveAll(lists.ListRemove.Contains);
                list.AddRange(lists.ListMerge);

                shuffle |= lists.Shuffle;
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

            return _merged[cacheName] = playlist;
        } catch (Exception ex) {
            CwlMod.WarnWithPopup<CustomPlaylist>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }

        return mold ?? EClass.Sound.plBlank;
    }

    public static CustomPlaylist MergeOverrides(IEnumerable<CustomPlaylist> overrides, string zoneName)
    {
        var lists = overrides.ToArray();
        var names = lists
            .Select(p => p.Name)
            .AddItem(zoneName)
            .ToArray();

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
            remove.ExceptWith(pl.ListMerge);

            shuffle |= pl.Shuffle;
        }

        var dict = Core.Instance.refs.dictBGM;
        merges.RemoveWhere(id => !dict.ContainsKey(id));
        remove.RemoveWhere(id => !dict.ContainsKey(id));

        playlist = new(string.Join("_", names.Distinct()), merges.ToArray(), remove.ToArray(), shuffle);
        return _cached[cacheName] = playlist;
    }

    private static bool ShouldPersistBossBgm()
    {
        return false;
    }

    private static string GetBasePlaylistName(string fullName)
    {
        var match = Regex.Match(fullName, "Playlist_(.*?)_Zone_");
        return match.Success ? match.Groups[1].Value : "";
    }

    [CwlPostLoad]
    private static void InvalidateBGM(GameIOProcessor.GameIOContext context)
    {
        var bgms = EClass.player.knownBGMs;
        bgms.RemoveWhere(id => !Core.Instance.refs.dictBGM.ContainsKey(id));
    }
}