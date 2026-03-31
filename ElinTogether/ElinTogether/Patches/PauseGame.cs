using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class PauseGame
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv.ShouldPauseGame), MethodType.Getter)]
    internal static void ShouldPauseGame_Modified(ref bool __result)
    {
        __result |= ActionModeCombat.Paused;

        if (!__result) {
            return;
        }

        // pause only if all players have no goal
        __result &= EClass.pc.party.members
            .Where(c => c.IsRemotePlayer)
            .All(c => c.ai is GoalRemote { child: null });
    }
}