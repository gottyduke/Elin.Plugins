using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Net;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class ActionModeCombat
{
    internal static bool InCombat = false;
    internal static bool Paused = false;
    private static bool WaitForSelf = false;

    [HarmonyPrefix, HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    internal static void CheckIfPauseNeeded()
    {
        if (!InCombat
            || NetSession.Instance.Connection is not ElinNetBase net
            || NetSession.Instance.CurrentPlayers.Count < 2) {
            Paused = false;
            WaitForSelf = false;
            return;
        }

        if (EClass.pc.HasNoGoal) {
            if (!Paused || !WaitForSelf) {
                Paused = true;
                WaitForSelf = true;
                Msg.SayGod("Decide your next action. ");
            }

            return;
        }

        var hasAnyoneToDecide = NetSession.Instance.CurrentPlayers.Any(n =>
            EClass.pc.party.members.Find(c => c.uid == n.CharaUid)?.ai is GoalRemote g && g.child is null);
        if (hasAnyoneToDecide) {
            if (!Paused || WaitForSelf) {
                Paused = true;
                WaitForSelf = false;
                Msg.SayGod("Wait for others to decide their next action. ");
            }

            return;
        }

        Paused = false;
        WaitForSelf = false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AIAct), nameof(AIAct.Tick))]
    static bool PreventImmediateAITick() => !Paused;
}