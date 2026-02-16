using System.Linq;
using ElinTogether.Elements;
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

        // early exit
        if (!__result) {
            return;
        }

        // pause only if all players have no goal
        __result &= NetSession.Instance.CurrentPlayers
            .Where(n => n.CharaUid != EClass.pc.uid)
            .All(n =>
                EClass.pc.party.members
                    .Find(c => c.uid == n.CharaUid)?.ai is not GoalRemote { child: not null });
    }
}