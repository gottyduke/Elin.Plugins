using Cwl.Patches.Charas;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class BackgroundContext(Chara chara) : ContextProviderBase
{
    public override string Name => "background";

    public override object? Build()
    {
        var id = chara.IsPC ? "player" : chara.id;

        var resourceKey = $"Emmersive/Characters/{id}.txt";

        var background = ResourceFetch.GetActiveResource(resourceKey);
        if (!background.IsEmpty()) {
            return background;
        }

        background = chara.IsPC
            ? EClass.player.GetBackgroundText()
            : BioOverridePatch.GetNpcBackground("", chara);

        ResourceFetch.SetActiveResource(resourceKey, background);

        return !background.IsEmpty()
            ? background
            : null;
    }
}