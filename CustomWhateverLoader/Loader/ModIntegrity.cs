using System.Collections.Generic;
using System.Linq;
using Cwl.API;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;

namespace Cwl;

public class ModIntegrity
{
    public static SerializableModPackage[] CurrentActivated =>
        BaseModManager.Instance.packages
            .Where(p => p.activated && !p.builtin)
            .Select(p => new SerializableModPackage {
                ModName = p.title,
                ModId = p.id,
            })
            .ToArray();

    [CwlPostLoad]
    private static void CheckModList(GameIOProcessor.GameIOContext context)
    {
        if (!context.Load<SerializableModPackage[]>(out var mods, "active_mods")) {
            return;
        }

        var missing = mods.Except(CurrentActivated).ToArray();
        if (missing.Length == 0) {
            return;
        }

        CoroutineHelper.Deferred(
            () => Dialog.YesNo(
                "cwl_warn_missing_mods".Loc(BuildMissingList(missing)),
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

    private static string BuildMissingList(IReadOnlyList<SerializableModPackage> missing)
    {
        using var sb = StringBuilderPool.Get();

        foreach (var mod in missing.Take(15)) {
            sb.AppendLine($"{mod.ModName}({mod.ModId})");
        }

        if (missing.Count > 15) {
            sb.AppendLine($"+ {missing.Count - 15}...");
        }

        return sb.ToString().TrimEnd();
    }
}