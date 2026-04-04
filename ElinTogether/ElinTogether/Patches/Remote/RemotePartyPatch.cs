using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class RemotePartyPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.IsPCParty), MethodType.Getter)]
    internal static bool OnGetPcParty(Chara __instance, ref bool __result)
    {
        __result = __instance.party is { } party && party.members.Contains(__instance);
        return false;
    }
}