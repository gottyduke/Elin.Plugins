using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using ReflexCLI.Attributes;

namespace Cwl;

[ConsoleCommandClassCustomizer("cwl")]
internal class CwlConsole
{
    [CwlContextMenu("CWL/BeggarBegone", "cwl_ui_vacate_beggar")]
    internal static string BegoneOfYouChicken()
    {
        // 23.149 changed beggar to chicken, what noa
        return BegoneOfYouInsertNameHere("chicken");
    }

    [ConsoleCommand("vacate_every")]
    internal static string BegoneOfYouInsertNameHere(string id)
    {
        var beggars = EClass.game.cards.globalCharas.Values
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
        EClass.core.SetReleaseMode(mode);
        EClass.core.debug.enable = enable;

        return $"debug : {enable}";
    }

    [ConsoleCommand("add_figures")]
    internal static void AddFigures(string refId)
    {
        var figure = ThingGen.Create("figure");
        var card = ThingGen.Create("figure3");
        figure.c_idRefCard = card.c_idRefCard = refId;

        var pc = EClass.pc;
        pc.DropThing(figure);
        pc.DropThing(card);
    }
}