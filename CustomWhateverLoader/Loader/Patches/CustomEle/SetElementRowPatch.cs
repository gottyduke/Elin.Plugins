using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.API;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Loader.Patches.Sources;
using HarmonyLib;
using MethodTimer;
using UnityEngine;

namespace Cwl.Loader.Patches.CustomEle;

[HarmonyPatch]
internal class SetElementRowPatch
{
    private static List<TypeInfo>? _declared;

    [Time]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SourceElement), nameof(SourceElement.SetRow))]
    internal static void OnSetRow(SourceElement.Row r)
    {
        if (!SourceInitPatch.SafeToCreate) {
            return;
        }

        // maybe add feat/act?
        if (r.group is not ("ABILITY" or "SPELL") || r.type is "") {
            return;
        }

        var unqualified = r.type;
        _declared ??= Resources.FindObjectsOfTypeAll<BaseUnityPlugin>()
            .SelectMany(p => p.GetType().Assembly.DefinedTypes)
            .Where(t => typeof(Act).IsAssignableFrom(t))
            .ToList();

        try {
            var qualified = _declared.FirstOrDefault(t => t.FullName == unqualified) ??
                            _declared.FirstOrDefault(t => t.Name == unqualified);
            if (qualified?.FullName is null) {
                return;
            }

            if (!SpriteSheet.dict.ContainsKey(r.alias) &&
                SpriteReplacer.dictModItems.TryGetValue(r.alias, out var icon)) {
                SpriteSheet.Add(icon.LoadSprite(name: r.alias, resizeWidth: 48, resizeHeight: 48));
            }

            r.type = qualified.FullName;
            CustomElement.Managed[r.id] = r;

            CwlMod.Log("cwl_log_custom_ele".Loc(r.id, r.type));
        } catch (Exception ex) {
            CwlMod.Error("cwl_error_qualify_ele".Loc(r.id, r.type, ex));
            // noexcept
        }
    }
}