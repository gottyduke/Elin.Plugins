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
            .Where(chara => chara is { id: "begger", c_altName: null })
            .ToArray();

        foreach (var chara in beggars) {
            chara.Destroy();
        }

        foreach (var chara in EClass.game.cards.listAdv.FindAll(chara => chara is { id: "begger", c_altName: null })) {
            chara.Destroy();
            EClass.game.cards.listAdv.Remove(chara);
        }
        
        return $"vacated {beggars.Length} beggar(s)";
    }
}