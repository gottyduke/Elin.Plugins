using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.Tool), MethodType.Getter)]
internal class CardToolPatch
{
    [HarmonyPrefix]
    internal static bool OnGetTool(Card __instance, ref Thing? __result)
    {
        if (__instance is not Chara chara) {
            return true;
        }

        __result = chara.held as Thing;
        return false;
    }
}