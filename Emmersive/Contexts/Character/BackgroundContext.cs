using System;
using Emmersive.Helper;
using EModding.Helper.Runtime.Exceptions;

namespace Emmersive.Contexts;

public class BackgroundContext(Chara chara) : ContextProviderBase
{
    public override string Name => "background";

    public override object? Build()
    {
        try {
            var resourceKey = $"Emmersive/Characters/{chara.UnifiedId}.txt";
            var background = ResourceFetch.GetActiveResource(resourceKey);

            if (background.IsEmptyOrNull) {
                if (chara.IsPC) {
                    background = EClass.player.GetBackgroundText();
                } else if (ModUtil.TryGetContent<CustomBiographyContent>($"Biography/{chara.id}", out var bio)) {
                    bio.RefreshCharaBio(chara);
                    background = bio.background;
                }

                if (background.IsEmptyOrNull) {
                    background = "em_ui_non_provided";
                }

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