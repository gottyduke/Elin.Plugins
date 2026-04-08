using System.Collections.Generic;
using Cwl.Helper.Extensions;

namespace ACS.API;

public static class AcsController
{
    public const string ReservedSuffix = "__acs_internal_reserved__";

    extension(Chara chara)
    {
        public SpriteData? GetAcsClip(string? clipName, bool snow = false)
        {
            clipName ??= chara.IsInCombat ? "combat" : "idle";
            if (chara.mapStr.TryGetValue("acs_override", out string overrideClip)) {
                clipName = overrideClip;
            }

            var suffixes = chara.sourceCard.replacer.suffixes;
            if (snow && suffixes.TryGetValue($"{clipName}_snow", out var snowClip)) {
                return snowClip;
            }

            return suffixes.GetValueOrDefault($"_acs_{clipName}");
        }
    }

    extension(Card owner)
    {
        public void PlayAcsClip(string clipName)
        {
            owner.mapStr.Set("acs_override", clipName);
        }

        public void StopAcsClip()
        {
            owner.mapStr.Remove("acs_override");
        }
    }

    extension(Thing thing)
    {
        public SpriteData? GetAcsClip(string? clipName, bool snow = false)
        {
            // TODO
            return null;
        }
    }
}