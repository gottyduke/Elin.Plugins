﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.API;
using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class BioOverridePatch
{
    private const int FallbackRowId = -1;
    private static readonly Dictionary<Chara, SerializableBioData> _cached = [];

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(Chara), "OnDeserialized"),
            AccessTools.Method(typeof(Chara), nameof(Chara.OnCreate)),
        ];
    }

    [HarmonyPostfix]
    internal static void OnCharaInstantiation(Chara __instance)
    {
        if (!CustomChara.BioOverride.TryGetValue(__instance.id, out var file) ||
            _cached.ContainsKey(__instance)) {
            return;
        }

        try {
            if (!ConfigCereal.ReadConfig<SerializableBioData>(file, out var bio) || bio is null) {
                return;
            }

            if (bio.Birthday != FallbackRowId) {
                __instance.bio.birthDay = bio.Birthday;
            }

            if (bio.Birthmonth != FallbackRowId) {
                __instance.bio.birthMonth = bio.Birthmonth;
            }

            if (bio.Birthyear != FallbackRowId) {
                __instance.bio.birthYear = bio.Birthyear;
            }

            ref var langWord = ref EMono.sources.langWord.map;
            langWord.TryAdd(FallbackRowId, new() {
                id = FallbackRowId,
                name_JP = "",
                name = "",
            });

            __instance.bio.idAdvDad = FallbackRowId;
            __instance.bio.idDad = langWord.NextUniqueKey();
            langWord[__instance.bio.idDad] = new() {
                id = __instance.bio.idDad,
                name_JP = bio.Dad_JP,
                name = bio.Dad,
            };

            __instance.bio.idAdvMom = FallbackRowId;
            __instance.bio.idMom = langWord.NextUniqueKey();
            langWord[__instance.bio.idMom] = new() {
                id = __instance.bio.idMom,
                name_JP = bio.Mom_JP,
                name = bio.Mom,
            };

            __instance.bio.idHome = langWord.NextUniqueKey();
            langWord[__instance.bio.idHome] = new() {
                id = __instance.bio.idHome,
                name_JP = bio.Birthplace_JP,
                name = bio.Birthplace,
            };

            __instance.bio.idLoc = langWord.NextUniqueKey();
            langWord[__instance.bio.idLoc] = new() {
                id = __instance.bio.idLoc,
                name_JP = bio.Birthlocation_JP,
                name = bio.Birthlocation,
            };

            _cached[__instance] = bio;
        } catch (Exception ex) {
            CwlMod.Warn("cwl_error_failure".Loc(ex));
            // noexcept
        }
    }

    [HarmonyPatch]
    internal class WindowBioSubPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(WindowChara), nameof(WindowChara.RefreshNote))]
        internal static IEnumerable<CodeInstruction> OnSetNpcBackgroundIl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchEndForward(
                    new CodeMatch(OpCodes.Ldstr, "???"),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                        typeof(UIText),
                        nameof(UIText.SetText),
                        [typeof(string)])))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    Transpilers.EmitDelegate(GetNpcBackground))
                .InstructionEnumeration();
        }

        private static string GetNpcBackground(string fallback, Chara c)
        {
            if (!_cached.TryGetValue(c, out var bio)) {
                return fallback;
            }

            return Lang.isJP
                ? bio.Background_JP
                : bio.Background;
        }
    }
}