using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using ReflexCLI.Attributes;
using ReflexCLI.UI;
using UnityEngine;

namespace Cwl;

[ConsoleCommandClassCustomizer("cwl")]
internal class CwlConsole : EMono
{
    private void Update()
    {
        if (scene.mode is not Scene.Mode.Title) {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.BackQuote)) {
            return;
        }

        if (ReflexUIManager.IsConsoleOpen()) {
            ReflexUIManager.StaticClose();
        } else {
            ReflexUIManager.StaticOpen();
        }
    }

    [CwlContextMenu("CWL/BeggarBegone", "cwl_ui_vacate_beggar")]
    internal static string BegoneOfYouChicken()
    {
        // 23.149 changed beggar to chicken, what noa
        return BegoneOfYouInsertNameHere("chicken");
    }

    [ConsoleCommand("gain_ability")]
    internal static string CharaGainAbility(string alias)
    {
        if (!sources.elements.alias.TryGetValue(alias, out var row)) {
            return $"no such alias {alias}";
        }

        pc.GainAbility(row.id);
        return $"learnt {alias}";
    }

    [ConsoleCommand("remove_all")]
    internal static string BegoneOfYouInsertNameHere(string id)
    {
        var beggars = game.cards.globalCharas.Values
            .Where(chara => chara.id == id)
            .ToArray();

        foreach (var chara in beggars) {
            chara.DestroyImmediate();
        }

        return "cwl_log_hobo_begone".Loc(beggars.Length);
    }

    [ConsoleCommand("enable_debug")]
    internal static string EnableDebug(bool enable = true)
    {
        var mode = enable ? ReleaseMode.Debug : ReleaseMode.Public;
        core.SetReleaseMode(mode);
        core.debug.enable = enable;

        return $"debug : {enable}";
    }

    [ConsoleCommand("add_figures")]
    internal static void AddFigures(string refId)
    {
        var figure = ThingGen.Create("figure");
        var card = ThingGen.Create("figure3");
        figure.c_idRefCard = card.c_idRefCard = refId;

        pc.DropThing(figure);
        pc.DropThing(card);
    }

    [ConsoleCommand("spawn_altar")]
    internal static string SpawnCustomAltar(string religionId)
    {
        if (!game.religions.dictAll.TryGetValue(religionId, out var religion)) {
            return $"cannot find religion {religionId}";
        }

        var altar = ThingGen.Create("altar");
        (altar.trait as TraitAltar)?.SetDeity(religion.id);

        var pos = pc.pos.GetNearestPoint(true, false, false);
        if (pos is not null) {
            _zone.AddCard(altar, pos).Install();
        }

        return $"spawned altar for {religionId}";
    }
}