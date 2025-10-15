using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Cwl.Helper.Exceptions;
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
        SceneReaction[]? reactions;

        try {
            reactions = JsonConvert.DeserializeObject<SceneReaction[]>(content);
        } catch (Exception ex) {
            EmMod.Warn<SceneDirector>($"failed to parse scene scripts\n{ex.Message}\n{content}");
            DebugThrow.Void(ex);
            return;
        }

        var maxDelay = 0f;

        foreach (var reaction in reactions ?? []) {
            maxDelay += reaction.delay;
            DoPopText(reaction.uid, reaction.text, reaction.duration, reaction.delay);
        }

        EmScheduler.SetScenePlayDelay(maxDelay + 1f);
    }
}