using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.bgm")]
public partial class CustomPlaylist
{
    private static ProgressIndicator? _bgmProgress;
    private static bool _killBgmProgress;
    private static string _lastBgmViewInfo = "";

    [ConsoleCommand("view")]
    internal static string EnableBGMView()
    {
        _killBgmProgress = false;
        
        _bgmProgress ??= ProgressIndicator.CreateProgress(
            () => new(GetCurrentPlaylistInfo()),
            () => _killBgmProgress,
            1f);

        _bgmProgress!.Pop!.text.alignment = TextAnchor.UpperLeft;

        return "enabled";
    }

    [ConsoleCommand("hide")]
    internal static string DisableBGMView()
    {
        _killBgmProgress = true;
        _bgmProgress = null;
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
        var pl = EClass.Sound.currentPlaylist;
        if (pl?.currentItem == null) {
            return _lastBgmViewInfo;
        }
        
        var current = pl.currentItem;
        var sb = new StringBuilder();
        
        sb.AppendLine(pl.name);
        sb.AppendLine($"shuffle: {pl.shuffle}");
        sb.AppendLine();

        foreach (var bgm in pl.list) {
            var marker = bgm == current ? "=>" : "   ";
            sb.AppendLine($"{marker} {bgm.data._name}");
        }

        sb.AppendLine();
        sb.AppendLine($@"{TimeSpan.FromSeconds(pl.playedTime):mm\:ss} / {TimeSpan.FromSeconds(current.data.clip.length):mm\:ss}");

        _lastBgmViewInfo = sb.ToString();
        return _lastBgmViewInfo;
    }
}