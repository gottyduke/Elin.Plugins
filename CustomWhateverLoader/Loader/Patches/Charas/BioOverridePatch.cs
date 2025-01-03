using System;
using System.Collections.Generic;
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Biography), nameof(Biography.Generate))]
    internal static void OnGenerate(Biography __instance, Chara c)
    {
        if (!CustomChara.BioOverride.TryGetValue(c.id, out var file) ||
            _cached.ContainsKey(c)) {
            return;
        }

        try {
            if (!ConfigCereal.ReadConfig<SerializableBioData>(file, out var bio) || bio is null) {
                return;
            }

            if (bio.Birthday != FallbackRowId) {
                __instance.birthDay = bio.Birthday;
            }

            if (bio.Birthmonth != FallbackRowId) {
                __instance.birthMonth = bio.Birthmonth;
            }

            if (bio.Birthyear != FallbackRowId) {
                __instance.birthYear = bio.Birthyear;
            }

            ref var langWord = ref EMono.sources.langWord.map;
            langWord.TryAdd(FallbackRowId, new() {
                id = FallbackRowId,
                name_JP = "",
                name = "",
            });

            __instance.idAdvDad = FallbackRowId;
            __instance.idDad = langWord.NextUniqueKey();
            langWord[__instance.idDad] = new() {
                id = __instance.idDad,
                name_JP = bio.Dad_JP,
                name = bio.Dad,
            };

            __instance.idAdvMom = FallbackRowId;
            __instance.idMom = langWord.NextUniqueKey();
            langWord[__instance.idMom] = new() {
                id = __instance.idMom,
                name_JP = bio.Mom_JP,
                name = bio.Mom,
            };

            __instance.idHome = langWord.NextUniqueKey();
            langWord[__instance.idHome] = new() {
                id = __instance.idHome,
                name_JP = bio.Birthplace_JP,
                name = bio.Birthplace,
            };

            __instance.idLoc = langWord.NextUniqueKey();
            langWord[__instance.idLoc] = new() {
                id = __instance.idLoc,
                name_JP = bio.Birthlocation_JP,
                name = bio.Birthlocation,
            };

            _cached[c] = bio;
        } catch (Exception ex) {
            CwlMod.Warn("cwl_error_failure".Loc(ex));
            // noexcept
        }
    }

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