using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Emmersive.Components;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using Newtonsoft.Json.Linq;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Emmersive.API.Plugins;

public partial class SceneDirector : EClass
{
    private const int MaxWrapperDepth = 4;

    private const float BaseBubbleSeconds = 3.5f;

    private static readonly string[] _knownWrappers = [
        "items",
        "reactions",
        "characters",
        "scenes",
        "scene",
        "lines",
        "data",
        "result",
        "output",
    ];

    private static readonly Regex _latinWord = new(@"[A-Za-z0-9'\-]+", RegexOptions.Compiled);

    public static SceneDirector Instance => field ??= new();

    public static bool FindSameMapChara(int uid, [NotNullWhen(true)] out Chara? chara)
    {
        chara = game.cards.Find(uid) ?? _map.charas.Find(c => c.uid == uid);
        return chara is { isDestroyed: false, ExistsOnMap: true };
    }

    public float Execute(string content, IReadOnlyCollection<string>? shownLines = null)
    {
        var reactions = TryParseReactions(content);
        if (reactions is not { Length: > 0 }) {
            throw new FormatException($"scene failed to parse scripts\n{content}");
        }

        if (shownLines is { Count: > 0 }) {
            reactions = [
                ..reactions.Where(r => !shownLines.Any(line => MemoryManager.IsContentSimilar(r.text, line))),
            ];
        }

        var max = EmConfig.Scene.MaxReactions.Value;
        if (reactions.Length > max) {
            reactions = reactions[..max];
        }

        var schedule = Schedule(
            reactions,
            player.uidChara,
            EmConfig.Scene.MinimalReactionDelay.Value,
            uid => FindSameMapChara(uid, out _));

        if (schedule.Count == 0) {
            EmMod.Warn<SceneDirector>($"no reachable target in scene\n{content}");
        }

        var sceneEnd = 0f;
        foreach (var (reaction, delay) in schedule) {
            var mult = BubbleDuration(reaction.text, reaction.duration);
            sceneEnd = Mathf.Max(sceneEnd, delay + mult * BaseBubbleSeconds);

            DoPopText(reaction.uid, reaction.text, mult, delay);

            if (reaction.emote is { } emote) {
                DoEmote(reaction.uid, emote, delay);
            }
        }

        EmScheduler.SetScenePlayDelay(sceneEnd);

        return sceneEnd;
    }

    internal static float BubbleDuration(string text, float modelDuration)
    {
        var units = Helper.StringHelper.Cjk.Char.Matches(text).Count + _latinWord.Matches(text).Count;
        var mult = Mathf.Clamp(0.6f + units / 12f, 1f, 3f);

        return Mathf.Max(mult, Mathf.Clamp(modelDuration, 0f, 3f));
    }

    internal static List<(SceneReaction Reaction, float Delay)> Schedule(
        IEnumerable<SceneReaction> reactions,
        int playerUid,
        float minimalGap,
        Func<int, bool> canPlay)
    {
        var schedule = new List<(SceneReaction, float)>();
        var delayReduced = 0f;
        var lastDelay = 0f;

        foreach (var reaction in reactions) {
            if (reaction.uid == playerUid) {
                delayReduced += BubbleDuration(reaction.text, reaction.duration) * BaseBubbleSeconds;
                continue;
            }

            if (!canPlay(reaction.uid)) {
                continue;
            }

            var delay = Mathf.Max(reaction.delay - delayReduced, 0f);
            if (schedule.Count > 0) {
                delay = Mathf.Max(delay, lastDelay + minimalGap);
            }

            lastDelay = delay;
            schedule.Add((reaction, delay));
        }

        return schedule;
    }

    internal static SceneReaction[]? TryParseReactions(string content)
    {
        content = StripMarkdownFence(content);

        if (content.IsEmptyOrNull) {
            return null;
        }

        JToken root;
        try {
            root = JToken.Parse(content);
        } catch {
            return null;
            // noexcept
        }

        var array = FindReactionArray(root);
        if (array is null) {
            return null;
        }

        var reactions = new List<SceneReaction>(array.Count);
        foreach (var item in array) {
            if (TryReadReaction(item, out var reaction)) {
                reactions.Add(reaction);
            }
        }

        return reactions.Count > 0 ? [..reactions] : null;
    }

    private static JArray? FindReactionArray(JToken? token, int depth = 0)
    {
        if (token is null || depth > MaxWrapperDepth) {
            return null;
        }

        switch (token) {
            case JArray array: {
                if (array.Any(IsReactionLike)) {
                    EmMod.Debug<SceneReaction>(depth == 0 ? "json_array mode" : "json_wrapped mode");
                    return array;
                }

                return array
                    .Select(item => FindReactionArray(item, depth + 1))
                    .FirstOrDefault(hit => hit is not null);
            }
            case JObject obj: {
                if (IsReactionLike(obj)) {
                    EmMod.Debug<SceneReaction>("json_single_item mode");
                    return [with(obj)];
                }

                foreach (var wrapper in _knownWrappers) {
                    if (FindReactionArray(obj[wrapper], depth + 1) is { } known) {
                        return known;
                    }
                }

                return obj
                    .Properties()
                    .Select(property => FindReactionArray(property.Value, depth + 1))
                    .FirstOrDefault(hit => hit is not null);
            }
            default:
                return null;
        }
    }

    private static bool IsReactionLike(JToken? token)
    {
        return token is JObject obj && obj["uid"] is not null && obj["text"] is not null;
    }

    private static bool TryReadReaction(JToken? token, [NotNullWhen(true)] out SceneReaction? reaction)
    {
        reaction = null;

        if (token is not JObject obj) {
            return false;
        }

        if (!TryReadNumber(obj["uid"], out var uid)) {
            return false;
        }

        var text = obj["text"]?.Type is JTokenType.String
            ? obj["text"]!.Value<string>()?.Trim()
            : null;
        if (text.IsEmptyOrNull) {
            return false;
        }

        reaction = new() {
            uid = (int)uid,
            text = text!,
            duration = TryReadNumber(obj["duration"], out var duration) ? Mathf.Clamp(duration, 0f, 3f) : 0f,
            delay = TryReadNumber(obj["delay"], out var delay) ? Mathf.Clamp(delay, 0f, 10f) : 0f,
            emote = TryReadEmote(obj["emote"]),
        };

        return true;
    }

    private static CharacterEmote? TryReadEmote(JToken? token)
    {
        if (token?.Type is not JTokenType.String) {
            return null;
        }

        var name = token.Value<string>()?.Trim();
        if (name.IsEmptyOrNull) {
            return null;
        }

        return Enum.TryParse<CharacterEmote>(name, true, out var emote) &&
               Enum.IsDefined(typeof(CharacterEmote), emote) &&
               !char.IsDigit(name![0])
            ? emote
            : null;
    }

    private static bool TryReadNumber(JToken? token, out float value)
    {
        value = 0f;

        switch (token?.Type) {
            case JTokenType.Integer or JTokenType.Float:
                value = token!.Value<float>();
                return true;
            case JTokenType.String:
                return float.TryParse(
                    token!.Value<string>(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value);
            default:
                return false;
        }
    }

    internal static string StripMarkdownFence(string content)
    {
        content = content.Trim();

        const string pattern = @"^```(?:json)?\s*([\s\S]*?)\s*```$";
        var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);

        content = match.Success
            ? match.Groups[1].Value.Trim()
            : content;

        content = content.Replace("```", "");
        return content;
    }
}