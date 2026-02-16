using System;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Chara), nameof(Chara.Speed), MethodType.Getter)]
internal static class RemoteSharedSpeedPatch
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

        // use shared speed if server has a meaningful value
        var sharedSpeed = NetSession.Instance.SharedSpeed;
        if (sharedSpeed <= 0) {
            return true;
        }

        __result = sharedSpeed;
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