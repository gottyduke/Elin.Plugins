using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
internal class RemoteBarterPatch
{
    [HarmonyPrefix]
    internal static bool OnBarter()
    {
        return NetSession.Instance.IsHost;
    }
}