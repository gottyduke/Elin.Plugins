using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaTaskRemoteEvent
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.SetAI))]
    internal static void OnSetAI(Chara __instance, ref AIAct g)
    {
        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // propagate every host event and client player event
        switch (connection) {
            // we are host, assign all active client charas as remote
            case ElinNetHost host when host.ActiveRemoteCharas.Values.Contains(__instance):
            // we are client, assign all other charas as remote
            case ElinNetClient when !__instance.IsPC:
                g = __instance.NetProfile.GoalDefault;
                break;
        }

        // switch case is inevitable for the mapping layer
        TaskArgsBase? args = g switch {
            NoGoal and not GoalRemote => NoTask.Default,
            TaskClean task => TaskCleanArgs.Create(task),
            TaskCullLife task => TaskCullLifeArgs.Create(task),
            TaskCut task => TaskCutArgs.Create(task),
            TaskDig task => TaskDigArgs.Create(task),
            TaskDrawWater task => TaskDrawWaterArgs.Create(task),
            TaskDump task => TaskDumpArgs.Create(task),
            TaskHarvest task => TaskHarvestArgs.Create(task),
            TaskMine task => TaskMineArgs.Create(task),
            TaskPlow task => TaskPlowArgs.Create(task),
            TaskPourWater task => TaskPourWaterArgs.Create(task),
            TaskWater task => TaskWaterArgs.Create(task),
            AI_PlayMusic task => AIPlayMusicArgs.Create(task),
            _ => null,
        };

        if (args is not null) {
            connection.Delta.AddRemote(new CharaTaskDelta {
                Owner = __instance,
                TaskArgs = args,
            });
        }
    }
}