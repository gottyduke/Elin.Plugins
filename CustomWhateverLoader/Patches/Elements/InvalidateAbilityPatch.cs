using System.Collections.Generic;
using Cwl.API.Custom;
using Cwl.LangMod;
using UnityEngine;

namespace Cwl.Patches.Elements;

internal class InvalidateAbilityPatch
{
    internal static void InvalidateElements(Chara chara)
    {
        var elements = EClass.sources.elements;
        var doReplace = false;
        List<string> safeActs = [];

        foreach (var act in chara.source.actCombat) {
            var actId = act.Split("/")[0];
            if (elements.alias.ContainsKey(actId)) {
                safeActs.Add(act);
            } else {
                doReplace = true;
                CwlMod.WarnWithPopup<CustomElement>("cwl_warn_fix_actCombat".Loc(actId, chara.id));
            }
        }

        if (doReplace) {
            chara.source.actCombat = safeActs.ToArray();
        }

        var list = chara._listAbility;
        if (list is null) {
            return;
        }

        for (var i = list.Count - 1; i >= 0; --i) {
            var id = Mathf.Abs(list[i]);
            if (elements.map.TryGetValue(id, out var ele) && ACT.dict.ContainsKey(ele.alias)) {
                continue;
            }

            list.RemoveAt(i);
            CwlMod.WarnWithPopup<CustomElement>("cwl_warn_fix_listAbility".Loc(id, chara.id));
        }

        if (list.Count == 0) {
            chara._listAbility = null;
        }
    }
}