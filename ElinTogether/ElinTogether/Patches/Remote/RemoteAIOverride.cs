using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class RemoteAIOverride
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.SetAI))]
    internal static void OnSetAI(Chara __instance, ref AIAct g)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        var newGoal = g;
        switch (connection) {
            case ElinNetHost host:
                // we are host
                if (host.ActiveRemoteCharas.Values.Contains(__instance)) {
                    // assign all active client charas as remote
                    newGoal = GoalRemote.Default;
                }

                break;
            case ElinNetClient client:
                // we are client
                if (!__instance.IsPC) {
                    // assign all other charas as remote
                    newGoal = GoalRemote.Default;
                }

                break;
        }

        // override if needed
        g = newGoal;

        // propagate every host event and client player event
        if (!connection.IsHost && !__instance.IsPC) {
            return;
        }

        if (g is Goal) {
            return;
        }

        if (g is TaskHarvest task) {
            connection.Delta.AddRemote(new CharaTaskDelta {
                Owner = __instance,
                TaskArgs = TaskHarvestArgs.Create(task),
            });
        }
    }
}