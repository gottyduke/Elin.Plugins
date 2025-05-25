using System;
using System.Collections.Generic;
using Cwl.API.Custom;
using Cwl.Helper.Extensions;
using Cwl.Helper.Runtime.Exceptions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static bool move_next_to(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var targetId);
        dm.RequiresActor(out var actor);

        if (dm.sequence.GetActor(targetId) is not { owner.chara: { } target }) {
            throw new DramaActionInvokeException("target");
        }

        var point = target.pos.GetNearestPoint(allowChara: true, ignoreCenter: true);
        actor.TryMove(point, false);

        return true;
    }

    public static bool move_tile(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var xOffset, out var yOffset);
        dm.RequiresActor(out var actor);

        var point = actor.pos.Add(new(xOffset.AsInt(0), yOffset.AsInt(0)));
        actor.TryMove(point, false);

        return true;
    }

    // nodiscard
    public static bool move_zone(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtleast(1);
        parameters.RequiresOpt(out var zoneId, out var lv);
        dm.RequiresActor(out var actor);

        var zoneName = $"{zoneId.Value}/{lv.Get("0")}";
        if (!CustomChara.ValidateZone(zoneName, out var targetZone) || targetZone is null) {
            return false;
        }

        actor.MoveZone(targetZone, new ZoneTransition {
            state = ZoneTransition.EnterState.RandomVisit,
        });

        return true;
    }

    public static bool play_anime(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var animeId);
        dm.RequiresActor(out var actor);

        if (!Enum.TryParse(animeId, out AnimeID anime)) {
            return false;
        }

        actor.PlayAnime(anime, true);

        return true;
    }

    public static bool play_effect(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var effectId);
        dm.RequiresActor(out var actor);

        actor.PlayEffect(effectId);

        return true;
    }

    public static bool play_emote(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtleast(1);
        parameters.RequiresOpt(out var emoteId, out var optDuration);
        dm.RequiresActor(out var actor);

        if (!Enum.TryParse(emoteId.Get("none"), out Emo emote)) {
            return false;
        }

        var duration = optDuration.Get("1f").AsFloat(1f);
        actor.ShowEmo(emote, duration, false);

        return true;
    }

    public static bool play_screen_effect(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var effectId);

        ScreenEffect.Play(effectId);

        return true;
    }

    public static bool pop_text(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var text);
        dm.RequiresActor(out var actor);

        actor.renderer.Say(text);

        return true;
    }

    public static bool portrait_set(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresOpt(out var portraitId);
        dm.RequiresPerson(out var owner);

        var id = portraitId.Get("cwl_not_provided");
        if (!portraitId.Provided || !Portrait.modPortraits.dict.ContainsKey(id)) {
            id = owner.chara.GetIdPortrait();
        }

        owner.idPortrait = id;

        return true;
    }
}