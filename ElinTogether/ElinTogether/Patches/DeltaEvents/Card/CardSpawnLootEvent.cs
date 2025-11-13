using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.SpawnLoot))]
internal class CardSpawnLootEvent
{
    [HarmonyPrefix]
    internal static bool OnCardSpawnLoot()
    {
        // we are clients, drop the update and wait for delta
        return NetSession.Instance.IsHost;
    }
}