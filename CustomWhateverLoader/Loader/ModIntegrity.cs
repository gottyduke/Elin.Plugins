using System.Linq;
using Cwl.API;
using Cwl.API.Processors;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl;

internal class ModIntegrity
{
    private static SerializableModPackage[] CurrentActivated => BaseModManager.Instance.packages
        .Where(p => p.activated && !p.builtin)
        .Select(p => new SerializableModPackage {
            modName = p.title,
            modId = p.id,
        })
        .ToArray();

    [CwlPostLoad]
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
                "cwl_warn_missing_mods".Loc(missing.Join(m => $"{m.modName}, {m.modId}", "\n")),
                () => EClass.scene.Init(Scene.Mode.Title),
                null,
                "cwl_warn_missing_mods_yes",
                "cwl_warn_missing_mods_no"),
            () => EClass.core.IsGameStarted);
    }

    [CwlPostSave]
    private static void SaveModList(GameIOProcessor.GameIOContext context)
    {
        context.Save(CurrentActivated, "active_mods");
    }
}