using System;
using System.IO;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.bgm")]
public class PlaylistViewer
{
    private static PlaylistViewer? _viewer;
    private readonly FastString _lastBgmViewInfo = new(256);

    private ProgressIndicator? _bgmProgress;
    private bool _detailedView;

    private string? DetailString => field ??= "cwl_ui_bgm_detail".Loc();
    private string? NextString => field ??= "cwl_ui_bgm_next".Loc();
    private string? PreviousString => field ??= "cwl_ui_bgm_last".Loc();
    private string? RebuildString => field ??= "cwl_ui_bgm_rebuild".Loc();
    private string? ShuffleString => field ??= "cwl_ui_bgm_shuffle".Loc();

    private GUIStyle? ButtonStyle =>
        field ??= new(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
        };

    private GUIStyle? ProgressBarStyle =>
        field ??= new(GUI.skin.box) {
            normal = {
                background = SpriteCreator.GetSolidColorTexture(new(0.2f, 0.6f, 0.8f)),
            },
        };

    private GUIStyle? ProgressBarBgStyle =>
        field ??= new(GUI.skin.box) {
            normal = {
                background = SpriteCreator.GetSolidColorTexture(new(0.5f, 0.5f, 0.5f)),
            },
        };

    [ConsoleCommand("view")]
    [CwlContextMenu("BGM/Show", "cwl_ui_bgm_view")]
    public static string EnableBGMView()
    {
        _viewer ??= new();
        _viewer.Show();
        return "enabled BGM panel";
    }

    [ConsoleCommand("hide")]
    public static string DisableBGMView()
    {
        _viewer?.Kill();
        return "disabled";
    }

    [ConsoleCommand("next")]
    public static string NextBGM()
    {
        var playlist = SoundManager.current.currentPlaylist;
        return PlayIndex(playlist.nextIndex);
    }

    [ConsoleCommand("last")]
    public static string LastBGM()
    {
        var playlist = SoundManager.current.currentPlaylist;
        return PlayIndex(playlist.nextIndex - 2 + playlist.list.Count);
    }

    [ConsoleCommand("shuffle")]
    public static string ShuffleBGM()
    {
        var playlist = SoundManager.current.currentPlaylist;
        playlist.Shuffle();
        return PlayIndex(EClass.rnd(playlist.list.Count));
    }

    public static string PlayIndex(int index)
    {
        var playlist = SoundManager.current.currentPlaylist;
        playlist.nextIndex = index % playlist.list.Count;
        playlist.Play();
        return playlist.currentItem.data._name;
    }

    [ConsoleCommand("add_known")]
    [CwlContextMenu("BGM/AddKnown", "cwl_ui_bgm_add_known")]
    public static string AddPlaylistToKnown()
    {
        var pl = SoundManager.current.currentPlaylist;
        var prev = EClass.player.knownBGMs.Count;
        EClass.player.knownBGMs.UnionWith(pl.ToInts());
        return $"added {EClass.player.knownBGMs.Count - prev} new BGM(s) to known list";
    }

    [ConsoleCommand("dump")]
    public static string DumpAllBGMData()
    {
        using var sb = StringBuilderPool.Get()
            .AppendLine("Playlist Dump");

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

    private void Show()
    {
        Kill();
        _bgmProgress ??= ProgressIndicator
            .CreateProgress(
                () => new(GetCurrentPlaylistInfo()),
                _ => !EClass.core.IsGameStarted,
                0.1f)
            .OnAfterGUI(DrawControlPanel);
    }

    private void Kill()
    {
        _bgmProgress?.Kill();
        _bgmProgress = null;
        _detailedView = false;
    }

    private string GetCurrentPlaylistInfo()
    {
        if (!EClass.core.IsGameStarted) {
            DisableBGMView();
            return "disabled";
        }

        var playlist = SoundManager.current.currentPlaylist;
        if (playlist?.currentItem == null) {
            return "disabled";
        }

        return _lastBgmViewInfo
            .Watch(WatchPlaylistString, BuildPlaylistString)
            .ToString();
    }

    private string WatchPlaylistString()
    {
        var pl = SoundManager.current.currentPlaylist;
        if (pl?.currentItem == null) {
            return "disabled";
        }

        var current = pl.currentItem;
        return $"{pl.UniqueString()}{_detailedView}{current.data.id}";
    }

    private string BuildPlaylistString()
    {
        var playlist = SoundManager.current.currentPlaylist;
        using var sb = StringBuilderPool.Get()
            .AppendLine(playlist.name)
            .AppendLine()
            .AppendLine($"{ShuffleString}\t {playlist.shuffle}")
            .AppendLine($"{DetailString}\t {_detailedView}")
            .AppendLine();

        for (var i = 0; i < playlist.list.Count; ++i) {
            var bgm = playlist.list[i];
            var bgmName = bgm == playlist.currentItem ? $"<b>{bgm.data._name}</b>" : bgm.data._name;
            sb.Append($"{i + 1:D2}\t{bgmName} ");

            if (_detailedView) {
                sb.AppendLine($"({bgm.data.name.Replace("BGM/", "")})");
            } else {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private void DrawControlPanel(ProgressIndicator progress)
    {
        var playlist = SoundManager.current.currentPlaylist;
        if (playlist == null || playlist.currentItem is not { data.clip.length: > 0f }) {
            return;
        }

        DrawProgressBar(playlist);
        DrawControlButtons();
    }

    private void DrawProgressBar(Playlist playlist)
    {
        var current = playlist.currentItem;
        var played = TimeSpan.FromSeconds(playlist.playedTime);
        var length = TimeSpan.FromSeconds(current.data.clip.length);
        var percent = (float)(played / length);

        var width = _bgmProgress!.Rect.width;
        var playedPercent = width * percent;
        var lengthPercent = Mathf.Max(width - width * percent - 10f, 0f);

        GUILayout.BeginHorizontal(GUILayout.Height(20f), GUILayout.MaxWidth(width));
        {
            GUILayout.Box($"<b>{played:mm\\:ss}</b>", GUILayout.ExpandWidth(false));
            GUILayout.Box("", ProgressBarStyle, GUILayout.MaxWidth(playedPercent), GUILayout.ExpandWidth(false));
            GUILayout.Box("", ProgressBarBgStyle, GUILayout.MaxWidth(lengthPercent), GUILayout.ExpandWidth(false));
            GUILayout.Box($"<b>{length:mm\\:ss}</b>", GUILayout.ExpandWidth(false));
        }
        GUILayout.EndHorizontal();
    }

    private void DrawControlButtons()
    {
        GUILayout.BeginHorizontal(GUILayout.Height(20f));
        {
            var detail = _detailedView ? $"<b>{DetailString}</b>" : DetailString;
            if (GUILayout.Button(detail, ButtonStyle)) {
                _detailedView = !_detailedView;
            }

            GUILayout.Space(2f);

            if (GUILayout.Button(RebuildString, ButtonStyle)) {
                CustomPlaylist.BuildPlaylists();
            }

            GUILayout.Space(2f);

            if (GUILayout.Button(ShuffleString, ButtonStyle)) {
                ShuffleBGM();
            }

            GUILayout.Space(2f);

            if (GUILayout.Button(PreviousString, ButtonStyle)) {
                LastBGM();
            }

            GUILayout.Space(2f);

            if (GUILayout.Button(NextString, ButtonStyle)) {
                NextBGM();
            }
        }
        GUILayout.EndHorizontal();
    }
}