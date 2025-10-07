using System;
using System.Linq;
using Cwl.LangMod;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class InvalidateAbilityPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(CharaAbility), nameof(CharaAbility.Refresh))]
    internal static Exception? OnInvalidateAbility(Exception? __exception, CharaAbility __instance)
    {
        if (__exception is null) {
            return null;
        }

        InvalidateAbilities(__instance.owner);

        try {
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
        var actCombat = chara.source.actCombat.ToList();
        actCombat.ForeachReverse(a => {
            var act = a.Split('/')[0];
            if ((chara.MainElement == Element.Void || elements.alias.ContainsKey(act)) &&
                ACT.dict.ContainsKey(act)) {
                return;
            }

            actCombat.Remove(a);
            CwlMod.WarnWithPopup<CharaAbility>("cwl_warn_fix_actCombat".Loc(a, chara.id));
        });

        if (actCombat.Count != chara.source.actCombat.Length) {
            chara.source.actCombat = actCombat.ToArray();
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