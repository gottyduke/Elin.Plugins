using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    /// <summary>
    ///     move_next_to(chara_id)
    /// </summary>
    public static bool move_next_to(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var targetId);
        dm.RequiresActor(out var actor);

        if (dm.sequence.GetActor(targetId) is not { owner.chara: { } target }) {
            throw new DramaActionInvokeException("target");
        }

        var point = target.pos.GetNearestPoint(allowChara: false, ignoreCenter: true);
        actor.TryMove(point, false);

        return true;
    }

    /// <summary>
    ///     move_to(x_offset, y_offset)
    /// </summary>
    public static bool move_tile(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var xOffset, out var yOffset);
        dm.RequiresActor(out var actor);

        var point = actor.pos.Add(new(xOffset.AsInt(0), yOffset.AsInt(0)));
        actor.TryMove(point, false);

        return true;
    }

    /// <summary>
    ///     move_to(x, y)
    /// </summary>
    public static bool move_to(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var x, out var y);
        dm.RequiresActor(out var actor);

        var point = new Point(x.AsInt(actor.pos.x), y.AsInt(actor.pos.z));
        actor.TryMove(point, false);

        return true;
    }

    /// <summary>
    ///     move_zone(zone_id, [zone_level 0])
    /// </summary>
    [CwlNodiscard]
    public static bool move_zone(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var zoneId, out var lv);
        dm.RequiresActor(out var actor);

        var zoneName = $"{zoneId.Value}@{lv.Get("0")}";
        if (!zoneName.ValidateZone(out var targetZone)) {
            return false;
        }

        actor.MoveZone(targetZone, new ZoneTransition {
            state = ZoneTransition.EnterState.RandomVisit,
        });

        return true;
    }

    /// <summary>
    ///     play_anime(AnimeID)
    /// </summary>
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

    /// <summary>
    ///     play_effect(effect_id)
    /// </summary>
    public static bool play_effect(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var effectId);
        dm.RequiresActor(out var actor);

        actor.PlayEffect(effectId);

        return true;
    }

    /// <summary>
    ///     play_emote(Emo)
    /// </summary>
    public static bool play_emote(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresAtLeast(1);
        parameters.RequiresOpt(out var emoteId, out var optDuration);
        dm.RequiresActor(out var actor);

        if (!Enum.TryParse(emoteId.Get("none"), out Emo emote)) {
            return false;
        }

        var duration = optDuration.AsFloat(1f);
        actor.ShowEmo(emote, duration, false);

        return true;
    }

    /// <summary>
    ///     play_screen_effect(ScreenEffect)
    /// </summary>
    public static bool play_screen_effect(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var effectId);

        ScreenEffect.Play(effectId);

        return true;
    }

    /// <summary>
    ///     pop_text(lang_text)
    /// </summary>
    public static bool pop_text(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var text);
        dm.RequiresActor(out var actor);

        actor.renderer.Say(text.lang());

        return true;
    }

    /// <summary>
    ///     portrait_set(portrait_id_or_short)
    /// </summary>
    public static bool portrait_set(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.RequiresOpt(out var portraitId);
        dm.RequiresPerson(out var owner);

        if (owner is null) {
            return false;
        }

        var id = portraitId.Get("cwl_not_provided");
        if (!portraitId.Provided) {
            id = owner.chara.GetIdPortrait();
        } else {
            var portrait = portraitId.Value;
            var actorId = owner.id.OrIfEmpty(owner.chara?.id);
            var ids = new HashSet<string>(StringComparer.Ordinal) {
                $"UN_{actorId}_{portrait}.png",
                $"{portrait}.png",
                $"{actorId}_{portrait}.png",
            };

            if (ids.FirstOrDefault(Portrait.allIds.Contains) is { } matchId) {
                id = matchId[..^4];
            }
        }

        owner.idPortrait = id;

        return true;
    }

    /// <summary>
    ///     show_book(Book/sister.txt)
    /// </summary>
    [CwlNodiscard]
    public static bool show_book(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        parameters.Requires(out var bookEntry);

        if (BookList.dict is null) {
            BookList.Init();
        }

        var bookData = bookEntry.Split('/');
        var category = bookData[0];
        var id = bookData[1];

        if (!BookList.dict!.TryGetValue(category, out var books) ||
            !books.TryGetValue(id, out var item)) {
            return false;
        }

        var isParchment = category == "Scroll";
        var bookUi = ui.AddLayer<LayerHelp>(isParchment ? "LayerParchment" : "LayerBook").book;
        bookUi.Show((isParchment ? "Scroll/" : "Book/") + item.id, null, item.title, item);

        return true;
    }
}