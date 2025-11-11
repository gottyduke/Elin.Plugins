using System;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Speed), MethodType.Getter)]
internal static class SharedSpeedPatch
{
    [HarmonyPrefix]
    internal static bool OnGetSpeed(Chara __instance, ref int __result)
    {
        if (!NetSession.Instance.HasActiveConnection) {
            return true;
        }

        if (!__instance.IsPC) {
            return true;
        }

        // always use shared speed from entire session
        __result = NetSession.Instance.SharedSpeed;
        return false;
    }

    extension(Chara chara)
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        internal int Stub_get_Speed()
        {
            throw new NotImplementedException("Chara.get_Speed");
        }
    }
}