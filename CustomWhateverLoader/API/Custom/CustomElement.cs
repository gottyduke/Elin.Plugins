using System;
using System.Collections;
using System.Collections.Generic;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Loader;
using Cwl.Loader.Patches;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomElement : Element
{
    internal static readonly Dictionary<int, SourceElement.Row> Managed = [];

    public static IEnumerable<SourceElement.Row> All => Managed.Values;

    // credits to 105gun
    [Time]
    internal static IEnumerator GainAbilityOnLoad()
    {
        if (!SafeSceneInitPatch.SafeToCreate) {
            yield break;
        }

        foreach (var element in All) {
            if (!element.tag.Contains("addEleOnLoad") ||
                player?.chara?.HasElement(element.id) is not false) {
                continue;
            }

            player.chara.GainAbility(element.id);
            CwlMod.Log($"added element {element.id} ");
        }
    }


    [Time]
    internal static void AddElement(SourceElement.Row r, string qualified)
    {
        try {
            if (!SpriteSheet.dict.ContainsKey(r.alias) &&
                SpriteReplacer.dictModItems.TryGetValue(r.alias, out var icon)) {
                SpriteSheet.Add(icon.LoadSprite(name: r.alias, resizeWidth: 48, resizeHeight: 48));
            }

            if (CwlConfig.QualifyTypeName) {
                r.type = qualified;
                CwlMod.Log("cwl_log_custom_ele".Loc(r.id, r.type));
            }

            Managed[r.id] = r;
        } catch (Exception ex) {
            CwlMod.Error("cwl_error_qualify_ele".Loc(r.id, r.type, ex));
            // noexcept
        }
    }
}