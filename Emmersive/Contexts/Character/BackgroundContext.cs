using System;
using Cwl.Helper.Exceptions;
using Cwl.Patches.Charas;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class BackgroundContext(Chara chara) : ContextProviderBase
{
    public override string Name => "background";

    public override object? Build()
    {
        try {
            var resourceKey = $"Emmersive/Characters/{chara.UnifiedId}.txt";
            var background = ResourceFetch.GetActiveResource(resourceKey);

            if (background.IsEmpty()) {
                background = chara.IsPC
                    ? EClass.player.GetBackgroundText()
                    : BioOverridePatch.GetNpcBackground("em_ui_non_provided", chara);
                ResourceFetch.SetActiveResource(resourceKey, background);
            }

            return background is null or "em_ui_non_provided"
                ? null
                : background;
        } catch (Exception ex) {
            DebugThrow.Void(ex);
            throw;
        }
    }
}