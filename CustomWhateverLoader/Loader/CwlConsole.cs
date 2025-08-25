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
}