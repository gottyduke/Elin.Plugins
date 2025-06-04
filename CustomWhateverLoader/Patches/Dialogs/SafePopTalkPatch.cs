using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class SafePopTalkPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TCText), nameof(TCText.Say))]
    internal static bool ShouldSafePop(TCText __instance, string s)
    {
        if (!s.IsEmpty()) {
            return true;
        }

        CwlMod.Warn<SafePopTalkPatch>("cwl_warn_pop_talk_empty".Loc(__instance.owner.Name));

        return false;
    }
}