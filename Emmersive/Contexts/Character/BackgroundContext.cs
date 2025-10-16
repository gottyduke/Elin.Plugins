using Cwl.Patches.Charas;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class BackgroundContext(Chara chara) : ContextProviderBase
{
    public override string Name => "background";

    public override object? Build()
    {
        var resourceKey = $"Emmersive/Characters/{chara.UnifiedId}.txt";
        string background;

        if (!ResourceFetch.HasActiveResource(resourceKey)) {
            background = chara.IsPC
                ? EClass.player.GetBackgroundText()
                : BioOverridePatch.GetNpcBackground("", chara);
            ResourceFetch.SetActiveResource(resourceKey, background);
        } else {
            background = ResourceFetch.GetActiveResource(resourceKey);
        }

        return background.IsEmpty()
            ? null
            : background;
    }
}