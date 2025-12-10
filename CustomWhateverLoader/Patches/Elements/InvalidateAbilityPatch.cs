using System;
using System.Linq;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class InvalidateAbilityPatch
{
    private static bool _retry;

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(CharaAbility), nameof(CharaAbility.Refresh))]
    internal static Exception? OnInvalidateAbility(Exception? __exception, CharaAbility __instance)
    {
        if (__exception is null) {
            return null;
        }

        if (_retry) {
            _retry = false;
            return __exception;
        }

        InvalidateAbilities(__instance.owner);

        try {
            _retry = true;
            __instance.Refresh();
        } catch (Exception ex) {
            return ex;
        }

        return null;
    }

    internal static void InvalidateAbilities(Chara chara)
    {
        var elements = EMono.sources.elements;

        // invalidate actCombat by alias
        var actCombats = chara.source.actCombat.ToList();
        var replacement = false;
        for (var i = actCombats.Count - 1; i >= 0; --i) {
            var actCombat = actCombats[i];
            var act = actCombat.Split('/')[0];

            if (act.IsEmptyOrNull) {
                actCombats.RemoveAt(i);
                replacement = true;
                continue;
            }

            if ((chara.MainElement == Element.Void || elements.alias.ContainsKey(act)) &&
                ACT.dict.ContainsKey(act)) {
                continue;
            }

            if (FuzzyLookup.TryFuzzyGetValue(elements.alias, act, out var fuzzyRow)) {
                actCombats[i] = actCombat.Replace(act, fuzzyRow.alias);
            } else {
                actCombats.RemoveAt(i);
                CwlMod.WarnWithPopup<CharaAbility>("cwl_warn_fix_actCombat".Loc(actCombat, chara.id));
            }

            replacement = true;
        }

        if (replacement) {
            chara.source.actCombat = actCombats.ToArray();
        }

        // invalidate ability by id, including DNA
        chara._listAbility?.ForeachReverse(a => {
            if (elements.map.ContainsKey(Mathf.Abs(a))) {
                return;
            }

            chara._listAbility.Remove(a);
            CwlMod.Warn<CharaAbility>("cwl_warn_fix_listAbility".Loc(a, chara.id));
        });
    }
}