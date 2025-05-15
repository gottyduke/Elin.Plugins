using System.Linq;
using Cwl.API.Attributes;
using ReflexCLI.Attributes;

namespace Cwl;

[ConsoleCommandClassCustomizer("cwl")]
internal class CwlConsole
{
    [ConsoleCommand("vacate_beggar")]
    [CwlContextMenu("CWL/BeggarBegone", "cwl_ui_vacate_beggar")]
    internal static string BegoneOfYouBeggars()
    {
        // Because noa wrote so
        // ReSharper disable once StringLiteralTypo
        var beggars = EClass.game.cards.globalCharas.Values
            .Where(chara => chara.id == "begger")
            .ToArray();

        foreach (var chara in beggars) {
            chara.Destroy();
        }

        // ReSharper disable once StringLiteralTypo
        foreach (var chara in EClass.game.cards.listAdv.FindAll(chara => chara.id == "begger")) {
            if (!chara.isDestroyed) {
                chara.Destroy();
            }

            EClass.game.cards.listAdv.Remove(chara);
        }

        return $"vacated {beggars.Length} beggar(s)";
    }
}