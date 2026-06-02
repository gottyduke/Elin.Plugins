using System.Collections.Generic;
using System.Linq;
using System.Text;
using EModding.API;

namespace EModding;

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

    public static void SetupEvent()
    {
        BaseModManager.SubscribeEvent<GameIOContext>(EVENT.PostLoad, CheckModList);
        BaseModManager.SubscribeEvent<GameIOContext>(EVENT.PostSave, SaveModList);
    }

    private static void CheckModList(GameIOContext context)
    {
        if (!context.Load<SerializableModPackage[]>("active_mods", out var mods)) {
            return;
        }

        var missing = mods.Except(CurrentActivated).ToArray();
        if (missing.Length == 0) {
            return;
        }

        CoroutineHelper.Deferred(
            () => Dialog.YesNo(
                "es_warn_missing_mods".lang(BuildMissingList(missing)),
                () => EClass.scene.Init(Scene.Mode.Title),
                null,
                "es_warn_missing_mods_yes",
                "es_warn_missing_mods_no"),
            () => EClass.core.IsGameStarted);
    }

    private static void SaveModList(GameIOContext context)
    {
        context.Save("active_mods", CurrentActivated);
    }

    private static string BuildMissingList(IReadOnlyList<SerializableModPackage> missing)
    {
        var sb = new StringBuilder();

        foreach (var mod in missing.Take(15)) {
            sb.AppendLine($"{mod.ModName}({mod.ModId})");
        }

        if (missing.Count > 15) {
            sb.AppendLine($"+ {missing.Count - 15}...");
        }

        return sb.ToString().TrimEnd();
    }
}