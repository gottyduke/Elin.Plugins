using System.Collections.Generic;
using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Helper.Extensions;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class ActionModeCombat
{
    internal static Dictionary<int, bool> EnemyVisibility { get; } = [];
    internal static bool Paused { get; private set; }
    internal static bool WaitForSelf { get; private set; }
    internal static bool Activated { get; private set; }

    // TODO: loc
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    internal static void CheckIfPauseNeeded()
    {
        EnemyVisibility.ForEach(kv => {
            if (NetSession.Instance.CurrentPlayers.All(p => p.CharaUid != kv.Key)) {
                EnemyVisibility.Remove(kv.Key);
            }
        });

        if (!NetSession.Instance.Rules.UseTurnBasedCombat ||
            EnemyVisibility.Values.All(v => !v) ||
            NetSession.Instance.Connection is null ||
            NetSession.Instance.CurrentPlayers.Count < 2) {
            if (Activated) {
                Msg.SayGod("Exit combat mode. ");
            }
            Activated = false;
            Paused = false;
            WaitForSelf = false;
            return;
        }

        if (!Activated) {
            EClass.pc.ai.Cancel();
            Msg.SayGod("Enter combat mode. ");
        }

        Activated = true;

        if (EClass.pc.HasNoGoal) {
            if (Paused && WaitForSelf) {
                return;
            }

            Paused = true;
            WaitForSelf = true;
            Msg.SayGod("Decide your next action. ");

            return;
        }

        var hasAnyoneToDecide = EClass.pc.party.members.Any(c => c.IsRemotePlayer && c.ai is GoalRemote { child: null });
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