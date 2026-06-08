using System.Reflection;
using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class TempSafeLoadPatch
{
    internal static MethodBase TargetMethod()
    {
        var inner = AccessTools.Inner(typeof(CustomCharaContent), "<>c");
        return AccessTools.Method(inner, "<OnGameLoad>b__16_2");
    }

    [HarmonyPrefix]
    internal static bool SafeLoadZone(ref string __result, Chara c)
    {
        __result = c.currentZone?.ZoneFullName ?? "";
        return false;
    }
}