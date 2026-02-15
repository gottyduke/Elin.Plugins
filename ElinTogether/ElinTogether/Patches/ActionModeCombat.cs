using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class ActionModeCombat
{
    internal static bool InCombat { get; set; }
    internal static bool Paused { get; set; }
    internal static bool WaitForSelf { get; private set; }

    // TODO: loc
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    internal static void CheckIfPauseNeeded()
    {
        if (!InCombat
            || NetSession.Instance.Connection is null
            || NetSession.Instance.CurrentPlayers.Count < 2) {
            Paused = false;
            WaitForSelf = false;
            return;
        }

        if (EClass.pc.HasNoGoal) {
            if (Paused && WaitForSelf) {
                return;
            }

            Paused = true;
            WaitForSelf = true;
            Msg.SayGod("Decide your next action. ");

            return;
        }

        var hasAnyoneToDecide = NetSession.Instance.CurrentPlayers.Any(n =>
            EClass.pc.party.members.Find(c => c.uid == n.CharaUid)?.ai is GoalRemote { child: null } g);
        if (hasAnyoneToDecide) {
            if (Paused && !WaitForSelf) {
                return;
            }

            Paused = true;
            WaitForSelf = false;
            Msg.SayGod("Wait for others to decide their next action. ");

            return;
        }

        Paused = false;
        WaitForSelf = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AIAct), nameof(AIAct.Tick))]
    private static bool PreventImmediateAITick()
    {
        return !Paused;
    }
}