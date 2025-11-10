using ElinTogether.Helper;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class NoPushPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TraitChara), nameof(TraitChara.CanBePushed), MethodType.Getter)]
    internal static void OnPushRemoteChara(Chara __instance, ref bool __result)
    {
        if (__instance.IsRemoteChara) {
            __result = false;
        }
    }
}