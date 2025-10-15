using Cwl.Patches.Charas;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class BackgroundContext(Chara chara) : ContextProviderBase
{
    private const string NotProvided = "<not provided>";
    public override string Name => "background";

    public override object? Build()
    {
        var resourceKey = $"Emmersive/Characters/{chara.UnifiedId}.txt";

        var background = ResourceFetch.GetActiveResource(resourceKey);
        if (background == NotProvided) {
            return null;
        }

        background = chara.IsPC
            ? EClass.player.GetBackgroundText()
            : BioOverridePatch.GetNpcBackground(NotProvided, chara);

        ResourceFetch.SetActiveResource(resourceKey, background);

        return background != NotProvided
            ? background
            : null;
    }
}