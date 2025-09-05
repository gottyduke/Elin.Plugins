using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Cwl.API.Attributes;
using Cwl.API.Custom;
using Cwl.API.Processors;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal static class RestoreCharaData
{
    internal static readonly Dictionary<Chara, SourceChara.Row> Restorable = [];

    [CwlCharaOnCreateEvent]
    internal static void SetOrRestoreCharaData(Chara chara)
    {
        if (!CustomChara.IsRestorable(chara, out var row)) {
            return;
        }

        Restorable[chara] = row;
        CustomChara._deferredUntilRestoration = true;
    }

    [CwlPreLoad]
    internal static void ClearData(GameIOProcessor.GameIOContext context)
    {
        CustomChara._deferredUntilRestoration = false;
        Restorable.Clear();
    }

    [CwlSceneInitEvent(Scene.Mode.StartGame)]
    internal static void PromptRestoration()
    {
        if (Restorable.Count == 0) {
            return;
        }

        Dialog.YesNo(
            "cwl_ui_chara_restore".Loc(BuildRestorationList()),
            RestoreChara,
            ResetChara,
            "cwl_ui_chara_restore_yes",
            "cwl_ui_chara_restore_no");
    }

    private static void RestoreChara()
    {
        foreach (var (chara, row) in Restorable) {
            chara.id = row.id;
            chara.SetCardOnDeserialized();
            CwlMod.Log<CustomChara>("cwl_log_chara_restore".Loc(row.id));
        }

        ClearData(null!);
    }

    private static void ResetChara()
    {
        foreach (var chara in Restorable.Keys) {
            chara.mapStr.Remove("cwl_source_chara_id");
        }

        ClearData(null!);
    }

    private static string BuildRestorationList()
    {
        var sb = new StringBuilder();

        foreach (var (chara, row) in Restorable.Take(15)) {
            sb.AppendLine($"{row.GetText()} {row.GetText("aka")}, lv {chara.LV}");
        }

        if (Restorable.Count > 15) {
            sb.AppendLine($"+ {Restorable.Count - 15}...");
        }

        return sb.ToString().TrimEnd();
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