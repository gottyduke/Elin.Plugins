using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.LangMod;
using Emmersive.Components;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace Emmersive.API.Plugins;

[Description("Core plugin that orchestrates scene play.")]
[EmPlugin]
public partial class SceneDirector : EClass
{
    public static bool FindSameMapChara(int uid, [NotNullWhen(true)] out Chara? chara)
    {
        chara = game.cards.Find(uid) ?? _map.charas.Find(c => c.uid == uid);
        return chara is { isDestroyed: false, ExistsOnMap: true };
    }

    public void Execute(string content)
    {
        var reactions = TryParseReactions(content);
        if (reactions is not { Length: > 0 }) {
            EmMod.Warn<SceneDirector>("em_ui_scene_parse_error".Loc(content));
            return;
        }

        var maxDelay = 0f;
        foreach (var reaction in reactions) {
            maxDelay += reaction.delay;
            DoPopText(reaction.uid, reaction.text, reaction.duration, reaction.delay);
        }

        EmScheduler.SetScenePlayDelay(maxDelay);
    }

    private SceneReaction[]? TryParseReactions(string content)
    {
        content = StripMarkdownFence(content);

        if (content.IsEmpty()) {
            return null;
        }

        // json_object mode, straight forward
        try {
            var array = JsonConvert.DeserializeObject<SceneReaction[]>(content);
            if (array is not null) {
                EmMod.Debug<SceneReaction>("json_object mode");
                return array;
            }
        } catch {
            // noexcept
        }

        // openai schema output
        try {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            if (dict is not null) {
                if (dict.TryGetValue("items", out var items)) {
                    EmMod.Debug<SceneReaction>("json_schema mode");
                    return JsonConvert.DeserializeObject<SceneReaction[]>(JsonConvert.SerializeObject(items));
                }

                if (dict.FirstOrDefault() is var firstCollection) {
                    EmMod.Debug<SceneReaction>("json_schema_forward mode");
                    return JsonConvert.DeserializeObject<SceneReaction[]>(JsonConvert.SerializeObject(firstCollection));
                }
            }
        } catch {
            // noexcept
        }

        // single item output, some wild models do this
        try {
            var single = JsonConvert.DeserializeObject<SceneReaction>(content);
            if (single is not null) {
                EmMod.Debug<SceneReaction>("json_single_item mode");
                return [single];
            }
        } catch {
            // noexcept
        }

        return null;
    }

    private string StripMarkdownFence(string content)
    {
        content = content.Trim();

        const string pattern = @"^```(?:json)?\s*([\s\S]*?)\s*```$";
        var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);

        return match.Success
            ? match.Groups[1].Value.Trim()
            : content;
    }
}