using System;
using System.Linq;
using Cwl.API;
using Cwl.API.Processors;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches;

[HarmonyPatch]
internal class ModIntegrityCheck
{
    private static SerializableModPackage[] CurrentActivated => BaseModManager.Instance.packages
        .Where(p => p.activated && !p.builtin)
        .Select(p => new SerializableModPackage {
            modName = p.title,
            modId = p.id,
        })
        .ToArray();

    internal static void Prepare()
    {
        GameIOProcessor.AddLoad(CheckModList, true);
        GameIOProcessor.AddSave(SaveModList, true);
    }

    private static void CheckModList(GameIOProcessor.GameIOContext context)
    {
        if (!context.Load<SerializableModPackage[]>(out var mods, "active_mods") || mods is null) {
            return;
        }

        var missing = mods.Except(CurrentActivated).ToArray();
        if (missing.Length == 0) {
            return;
        }

        CoroutineHelper.Deferred(
            () => Dialog.YesNo(
                "cwl_warn_missing_mods".Loc(missing.Join(m => $"{m.modName}, {m.modId}", Environment.NewLine)),
                () => EClass.scene.Init(Scene.Mode.Title),
                delegate { },
                "cwl_warn_missing_mods_yes",
                "cwl_warn_missing_mods_no"),
            () => EClass.core.IsGameStarted);
    }

    private static void SaveModList(GameIOProcessor.GameIOContext context)
    {
        context.Save(CurrentActivated, "active_mods");
    }
}