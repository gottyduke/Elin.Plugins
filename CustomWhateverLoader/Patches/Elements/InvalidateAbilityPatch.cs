using Cwl.API.Attributes;
using UnityEngine;

namespace Cwl.Patches.Elements;

internal class InvalidateAbilityPatch
{
    [CwlCharaOnCreateEvent]
    internal static void InvalidateAbilities(Chara chara)
    {
        var abilities = chara._listAbility;
        if (abilities is null) {
            return;
        }

        var elements = EMono.sources.elements.map;
        abilities.ForeachReverse(a => {
            if (elements.ContainsKey(Mathf.Abs(a))) {
                return;
            }

            abilities.Remove(a);
            CwlMod.Warn<CharaAbility>($"removed invalid ability '{a}' from {chara.id}");
        });
    }
}