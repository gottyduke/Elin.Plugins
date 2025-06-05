using Cwl.API.Attributes;

namespace Cwl.Patches.Recipes;

internal class ForceRarityPatch
{
    [CwlThingOnCreateEvent]
    internal static void OnResetRarity(Thing __instance)
    {
        var row = __instance.sourceCard;
        if (row is null) {
            return;
        }

        if (row.tag.Contains("forceRarity")) {
            __instance.ChangeRarity(row.quality.ToEnum<Rarity>());
        }
    }
}