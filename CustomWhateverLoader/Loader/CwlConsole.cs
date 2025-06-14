using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using ReflexCLI.Attributes;

namespace Cwl;

[ConsoleCommandClassCustomizer("cwl")]
internal class CwlConsole
{
    [ConsoleCommand("vacate_beggar")]
    [CwlContextMenu("CWL/BeggarBegone", "cwl_ui_vacate_beggar")]
    internal static string BegoneOfYouBeggars()
    {
        // 23.149 changed beggar to chicken, what noa
        var beggars = EClass.game.cards.globalCharas.Values
            .Where(chara => chara.id == "chicken")
            .ToArray();

        foreach (var chara in beggars) {
            chara.DestroyImmediate();
        }

        return "cwl_log_hobo_begone".Loc(beggars.Length);
    }
}