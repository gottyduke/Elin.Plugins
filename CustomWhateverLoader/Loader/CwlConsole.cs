using System.Linq;
using Cwl.API.Attributes;
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
        var destroyed = 0;

        // 23.149 changed beggar to chicken, what noa
        var beggars = EClass.game.cards.globalCharas.Values
            .Where(chara => chara.id == "chicken")
            .ToArray();

        foreach (var chara in beggars) {
            chara.Destroy();
            destroyed++;
        }

        // ReSharper disable once StringLiteralTypo
        foreach (var chara in EClass.game.cards.listAdv.FindAll(chara => chara.id == "chicken")) {
            if (!chara.isDestroyed) {
                chara.Destroy();
                destroyed++;
            }

            EClass.game.cards.listAdv.Remove(chara);
        }

        return "cwl_log_hobo_begone".Loc(destroyed);
    }
}