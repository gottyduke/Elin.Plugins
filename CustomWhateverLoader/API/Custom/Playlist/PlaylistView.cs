﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.bgm")]
public partial class CustomPlaylist
{
    private static ProgressIndicator? _bgmProgress;
    private static bool _killBgmProgress;
    private static readonly FastString _lastBgmViewInfo = new(128);
    private static bool _detailedView;

    [ConsoleCommand("view")]
    internal static string EnableBGMView()
    {
        if (!_killBgmProgress && _bgmProgress != null) {
            _detailedView = !_detailedView;
            return $"detailed view: {_detailedView}";
        }

        _killBgmProgress = false;
        if (_bgmProgress == null) {
            _bgmProgress = ProgressIndicator.CreateProgress(
                () => new(GetCurrentPlaylistInfo()),
                () => _killBgmProgress,
                1f);
        }

        return "enabled, use this command again to toggle detailed view";
    }

    [ConsoleCommand("hide")]
    internal static string DisableBGMView()
    {
        _killBgmProgress = true;
        _bgmProgress = null;
        _detailedView = false;
        return "disabled";
    }

    [ConsoleCommand("next")]
    internal static string NextBGM()
    {
        var pl = EClass.Sound.currentPlaylist;
        pl.Play();
        return pl.currentItem.data._name;
    }

    [ConsoleCommand("last")]
    internal static string LastBGM()
    {
        var pl = EClass.Sound.currentPlaylist;
        pl.nextIndex = (pl.nextIndex - 2 + pl.list.Count) % pl.list.Count;
        pl.Play();
        return pl.currentItem.data._name;
    }

    [ConsoleCommand("shuffle")]
    internal static string ShuffleBGM()
    {
        var pl = EClass.Sound.currentPlaylist;
        pl.nextIndex = EClass.rnd(pl.list.Count);
        pl.Shuffle();
        pl.Play();
        return pl.currentItem.data._name;
    }

    [ConsoleCommand("dump")]
    [SuppressMessage("ReSharper", "Unity.UnknownResource")]
    internal static string DumpAllBGMData()
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("Playlist Dump");

        var pls = Resources.LoadAll<Playlist>($"{DataLoader.SoundPathEntry}BGM/Playlist");
        foreach (var playlist in pls) {
            sb.AppendLine($"+ {playlist.name,-30}[{playlist.list.Count}]");
            foreach (var data in playlist.list.Select(item => item.data)) {
                if (data?.clip == null) {
                    continue;
                }

                sb.AppendLine($"\t+ {data.name,-40} {data.clip.length:0.##}s - {data.clip.frequency}Hz x{data.clip.channels}");
            }
        }

        sb.AppendLine("Playlist Item\n|id|sound name|bgm name|\n|-|-|-|");
        foreach (var (id, bgm) in Core.Instance.refs.dictBGM) {
            sb.AppendLine($"|{id}|{bgm.name}|{bgm._name}|");
        }

        CwlMod.Log<CustomPlaylist>(sb);

        var dump = $"{CorePath.rootExe}/playlists.md";
        File.WriteAllText(dump, sb.ToString());

        return $"output has been dumped to {dump.NormalizePath()}";
    }

    private static string GetCurrentPlaylistInfo()
    {
        if (!EClass.core.IsGameStarted) {
            DisableBGMView();
            return "disabled";
        }

        var pl = EClass.Sound.currentPlaylist;
        if (pl?.currentItem == null) {
            return "disabled";
        }

        var current = pl.currentItem;
        var played = TimeSpan.FromSeconds(pl.playedTime);
        var length = TimeSpan.FromSeconds(current.data.clip.length);
        return _lastBgmViewInfo
            .Watch(WatchPlaylistString, BuildPlaylistString)
            .With($@"{played:mm\:ss} / {length:mm\:ss}");
    }

    private static string WatchPlaylistString()
    {
        var pl = EClass.Sound.currentPlaylist;
        if (pl?.currentItem == null) {
            return "disabled";
        }

        var current = pl.currentItem;
        return $"{pl.UniqueString()}{_detailedView}{current.data.id}";
    }

    private static string BuildPlaylistString()
    {
        var newPl = EClass.Sound.currentPlaylist;
        var sb = new StringBuilder();

        sb.AppendLine(newPl.name);
        sb.AppendLine();
        sb.AppendLine("cwl_bgm_shuffle".Loc(newPl.shuffle));
        sb.AppendLine("cwl_bgm_stream".Loc(CwlConfig.SeamlessStreaming));
        sb.AppendLine("cwl_bgm_detail".Loc(_detailedView));
        sb.AppendLine();

        for (var i = 0; i < newPl.list.Count; ++i) {
            var bgm = newPl.list[i];
            var bgmName = bgm == newPl.currentItem ? $"<b>{bgm.data._name}</b>" : bgm.data._name;
            sb.Append($"{i + 1:D2}\t{bgmName} ");

            if (_detailedView) {
                sb.AppendLine($"({bgm.data.name.Replace("BGM/", "")})");
            } else {
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }
}