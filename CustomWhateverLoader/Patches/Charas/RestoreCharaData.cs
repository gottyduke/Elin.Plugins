using System;
using System.Runtime.Serialization;
using Cwl.API.Attributes;
using Cwl.API.Custom;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal static class RestoreCharaData
{
    [CwlCharaOnCreateEvent]
    internal static void SetOrRestoreCharaData(Chara chara)
    {
        if (!CustomChara.IsRestorable(chara, out var row)) {
            return;
        }

        chara.id = row.id;
        chara.SetCardOnDeserialized();
        CwlMod.Log<CustomChara>("cwl_log_chara_restore".Loc(row.id));
    }

    extension(Card card)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        [HarmonyPatch(typeof(Card), "_OnDeserialized")]
        internal void SetCardOnDeserialized(StreamingContext? context = null)
        {
            throw new NotImplementedException(".Card._OnDeserialized");
        }
    }
}