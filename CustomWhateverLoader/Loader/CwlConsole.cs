using System.Linq;
using ReflexCLI.Attributes;

namespace Cwl;

[ConsoleCommandClassCustomizer("cwl")]
internal class CwlConsole
{
    [ConsoleCommand("vacate_beggar")]
    internal static string BegoneOfYouBeggars()
    {
        // Because noa wrote so
        // ReSharper disable once StringLiteralTypo
        var beggars = EClass.game.cards.globalCharas.Values
            .Where(chara => chara.id == "begger" && chara.c_altName is null && chara.Aka == chara.NameSimple)
            .ToArray();

        foreach (var chara in beggars) {
            chara.Destroy();
        }

        return $"vacated {beggars.Length} beggars";
    }
}