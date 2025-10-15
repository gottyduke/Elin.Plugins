using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Emmersive.API.Plugins;
using Emmersive.Components;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace Emmersive.API.Services.SceneDirector;

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
            EmMod.Warn<SceneDirector>($"failed to parse scene scripts\n{content}");
            return;
        }

        var maxDelay = 1f;
        foreach (var reaction in reactions) {
            maxDelay += reaction.delay;
            DoPopText(reaction.uid, reaction.text, reaction.duration, reaction.delay);
        }

        EmScheduler.SetScenePlayDelay(maxDelay);
    }

    private SceneReaction[]? TryParseReactions(string content)
    {
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
            if (dict is not null && dict.TryGetValue("items", out var items)) {
                EmMod.Debug<SceneReaction>("json_schema mode");
                return JsonConvert.DeserializeObject<SceneReaction[]>(JsonConvert.SerializeObject(items));
            }
        } catch {
            // noexcept
        }

        // single item output, some wild models do this
        try {
            var single = JsonConvert.DeserializeObject<SceneReaction>(content);
            if (single != null) {
                EmMod.Debug<SceneReaction>("single item json");
                return [single];
            }
        } catch {
            // noexcept
        }

        return null;
    }
}