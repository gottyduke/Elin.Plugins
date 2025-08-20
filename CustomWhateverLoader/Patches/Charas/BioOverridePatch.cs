using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API;
using Cwl.API.Attributes;
using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class BioOverridePatch
{
    private const int FallbackRowId = -1;
    private static readonly Dictionary<string, SerializableBioData> _cached = [];

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WindowChara), nameof(WindowChara.RefreshNote))]
    internal static IEnumerable<CodeInstruction> OnSetNpcBackgroundIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new(OpCodes.Ldstr, "???"),
                new OperandContains(OpCodes.Callvirt, nameof(UIText.SetText)))
            .EnsureValid("set ??? background")
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(GetNpcBackground))
            .InstructionEnumeration();
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WindowChara), nameof(WindowChara.RefreshInfo))]
    internal static void OnRefreshInfo(WindowChara __instance)
    {
        if (!_cached.TryGetValue(__instance.chara.HashKey(), out var bio)) {
            return;
        }

        var mom = (Lang.isJP ? bio.Mom_JP : bio.Mom).Split("@");
        var dad = (Lang.isJP ? bio.Dad_JP : bio.Dad).Split("@");

        var info = __instance.transform.GetFirstNestedChildWithName("Content View/Profile/info");
        if (mom.Length > 1) {
            info?.GetFirstChildWithName("mom")?.GetComponent<UIText>()?.SetText(mom[1]);
        }

        if (dad.Length > 1) {
            info?.GetFirstChildWithName("dad")?.GetComponent<UIText>()?.SetText(dad[1]);
        }
    }

    [CwlCharaOnCreateEvent]
    internal static void OnCharaInstantiation(Chara chara)
    {
        if (!CustomChara.BioOverride.TryGetValue(chara.id, out var file) ||
            _cached.ContainsKey(chara.HashKey())) {
            return;
        }

        try {
            if (!ConfigCereal.ReadConfig<SerializableBioData>(file, out var bio) || bio is null) {
                return;
            }

            if (bio.Birthday != FallbackRowId) {
                chara.bio.birthDay = bio.Birthday;
            }

            if (bio.Birthmonth != FallbackRowId) {
                chara.bio.birthMonth = bio.Birthmonth;
            }

            if (bio.Birthyear != FallbackRowId) {
                chara.bio.birthYear = bio.Birthyear;
            }

            var langWord = EMono.sources.langWord.map;
            langWord.TryAdd(FallbackRowId, new() {
                id = FallbackRowId,
                name_JP = "",
                name = "",
            });

            chara.bio.idAdvDad = FallbackRowId;
            chara.bio.idDad = langWord.NextUniqueKey();
            langWord[chara.bio.idDad] = new() {
                id = chara.bio.idDad,
                name_JP = bio.Dad_JP.Split("@")[0],
                name = bio.Dad.Split("@")[0],
            };

            chara.bio.idAdvMom = FallbackRowId;
            chara.bio.idMom = langWord.NextUniqueKey();
            langWord[chara.bio.idMom] = new() {
                id = chara.bio.idMom,
                name_JP = bio.Mom_JP.Split("@")[0],
                name = bio.Mom.Split("@")[0],
            };

            chara.bio.idHome = langWord.NextUniqueKey();
            langWord[chara.bio.idHome] = new() {
                id = chara.bio.idHome,
                name_JP = bio.Birthplace_JP,
                name = bio.Birthplace,
            };

            chara.bio.idLoc = langWord.NextUniqueKey();
            langWord[chara.bio.idLoc] = new() {
                id = chara.bio.idLoc,
                name_JP = bio.Birthlocation_JP,
                name = bio.Birthlocation,
            };

            _cached[chara.HashKey()] = bio;
        } catch (Exception ex) {
            CwlMod.WarnWithPopup<CustomChara>("cwl_error_failure".Loc(ex.Message), ex);
            // noexcept
        }
    }

    private static string GetNpcBackground(string fallback, Chara c)
    {
        if (!_cached.TryGetValue(c.HashKey(), out var bio)) {
            return fallback;
        }

        return Lang.isJP
            ? bio.Background_JP
            : bio.Background;
    }
}