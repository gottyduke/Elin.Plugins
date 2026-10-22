using System.Collections.Generic;
using System.Text.RegularExpressions;
using Emmersive.API.ThirdParty;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using UnityEngine;

namespace Emmersive.API.Plugins;

public partial class SceneDirector
{
    private static readonly Regex _richTextTag = new("<[^>]+>", RegexOptions.Compiled);

    internal RecentLineGuard RecentLines { get; } = new();

    public void DoPopText(int uid, string content, float duration = 0f, float delay = 0f, bool isPlayer = false)
    {
        DoPopText(uid, content, duration, delay, isPlayer, isPlayer);
    }

    internal void DoPopText(int uid, string content, float duration, float delay, bool isPlayer, bool typed)
    {
        if (!FindSameMapChara(uid, out var chara)) {
            return;
        }

        if (!isPlayer) {
            content = chara.ApplyTone(content);
        }

        content = content.Trim();

        if (content.Length >= 2 && content.StartsWith('~') && content.EndsWith('~')) {
            content = $"*{content[1..^1]}*";
        }

        // gpt prefers this quote
        content = content.Replace('’', '\'');

        if (duration <= 0f) {
            duration = BubbleDuration(content, 0f);
        }

        var segments = new List<(string Text, bool Gesture)>();
        foreach (Match match in Regex.Matches(content, @"(\*[^*]+\*)|([^\*]+)")) {
            var text = match.Value.Trim();
            if (!text.IsEmptyOrNull) {
                segments.Add((text, text.StartsWith("*") && text.EndsWith("*")));
            }
        }

        CoroutineHelper.Deferred(() => {
            foreach (var (text, gesture) in segments) {
                PopText(text, gesture);
            }
        }, delay);

        EmMod.Debug<SceneDirector>($"{chara.Name}: (delay:{delay} duration:{duration}): {content}");

        return;

        void PopText(string text, bool gesture)
        {
            if (chara is not { isDestroyed: false, ExistsOnMap: true }) {
                return;
            }

            var profile = chara.Profile;
            if (!pc.CanSee(chara)) {
                return;
            }

            Color color;
            if (gesture) {
                color = Msg.colors.Ono;
            } else {
                color = Msg.colors.Talk;
                text = text.Replace("&", "");
            }

            var memory = EmConfig.Memory.Enabled.Value;
            var clean = CleanLine(text, gesture);
            if (!typed &&
                (RecentLines.IsRepeat(chara.uid, clean, gesture, Time.unscaledTime) ||
                 (memory && MemoryManager.Instance.HasRecentTalk(chara, text)))) {
                EmMod.Debug<SceneDirector>($"dropped repeated line from {chara.NameSimple}: {text}");
                return;
            }

            var line = new SceneLineArgs {
                Chara = chara,
                Text = clean,
                IsGesture = gesture,
                IsPlayer = isPlayer,
                Duration = duration,
            };

            EmEvent.RaiseSceneLine(line);
            PublishSceneLine(line);

            if (line.Suppress || line.Text.IsWhiteSpaceOrNull) {
                profile.ResetTalkCooldown();
                return;
            }

            if (line.Text != clean) {
                text = gesture ? $"*{line.Text}*" : line.Text;
            }

            RecentLines.Add(chara.uid, line.Text, Time.unscaledTime);
            if (memory) {
                MemoryManager.Instance.RecordTalk(chara, text);
                if (!typed) {
                    MemoryManager.Instance.MarkSaid(chara, text);
                }
            }

            Msg.SetColor(color);

            var logText = gesture ? text : text.Bracket();
            if (EmConfig.Scene.PrefixSpeakerName.Value) {
                logText = $"{chara.NameSimple}: {logText}";
            }

            chara.Say(logText);

            var popText = text.Wrap();
            if (popText.Length > 0 && popText[0] is '@' or '^' or '|') {
                popText = "​" + popText;
            }

            if (profile.UsePopFeed && WidgetFeed.Instance != null) {
                WidgetFeed.Instance.SayRaw(chara, popText);
            } else {
                chara.HostRenderer.Say(popText, duration: line.Duration);
            }

            profile.ResetTalkCooldown();
        }
    }

    [ElinPostLoad]
    private static void ClearRecentLines(GameIOContext context)
    {
        Instance.RecentLines.Clear();
    }

    internal static string CleanLine(string text, bool gesture)
    {
        if (gesture) {
            text = text.Trim('*');
        }

        return _richTextTag.Replace(text, "").Trim();
    }

    private static void PublishSceneLine(SceneLineArgs line)
    {
        var data = new Dictionary<string, object> {
            ["uid"] = line.Chara.uid,
            ["name"] = line.Chara.NameSimple,
            ["text"] = line.Text,
            ["gesture"] = line.IsGesture,
            ["player"] = line.IsPlayer,
            ["duration"] = line.Duration,
        };

        BaseModManager.PublishEvent<IDictionary<string, object>>(EmEvent.SceneLine, data);

        if (data.TryGetValue("text", out var rewritten) && rewritten is string text) {
            line.Text = text;
        }
    }
}