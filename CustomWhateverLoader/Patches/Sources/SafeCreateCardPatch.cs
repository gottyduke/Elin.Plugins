using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class SafeCreateCardPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [Time]
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(SourceCard), nameof(SourceCard.AddRow))]
    internal static Exception? RethrowCreateInvoke(Exception? __exception, SourceCard __instance, CardRow row, bool isChara)
    {
        if (__exception is null) {
            return null;
        }

        CwlMod.WarnWithPopup<SourceCard>("cwl_warn_card_create".Loc(row.GetType().FullName, row.id, row.name, __exception.Message), __exception);

        // noexcept
        return null;
    }
}