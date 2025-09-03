using Cwl.API.Custom;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class RestoreCharaData
{
    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.SetSource))]
    internal static void SetOrRestoreCharaData(Chara __instance)
    {
        if (!CustomChara.IsRecoverable(__instance, out var id)) {
            return;
        }

        __instance.id = id;
        CwlMod.Log<RestoreCharaData>("cwl_log_chara_restore".Loc(id));
    }
}