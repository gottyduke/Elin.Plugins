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

        // a switch case is inevitable for the mapping layer
        TaskArgsBase args = g switch {
            // no goal/reset
            NoGoal and not GoalRemote => NoTask.Default,
            // task
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
            // ai
            AI_ArmPillow ai => AIArmPillowArgs.Create(ai),
            AI_AttackHome ai => AIAttackHomeArgs.Create(ai),
            AI_Bladder ai => AIBladderArgs.Create(ai),
            AI_Churyu ai => AIChuryuArgs.Create(ai),
            AI_Clean ai => AICleanArgs.Create(ai),
            AI_Cook ai => AICookArgs.Create(ai),
            AI_Craft_Snowman ai => AICraftSnowmanArgs.Create(ai),
            AI_Craft ai => AICraftArgs.Create(ai),
            AI_Dance ai => AIDanceArgs.Create(ai),
            AI_Deconstruct ai => AIDeconstructArgs.Create(ai),
            AI_Drink ai => AIDrinkArgs.Create(ai),
            AI_Eat ai => AIEatArgs.Create(ai),
            AI_Equip ai => AIEquipArgs.Create(ai),
            AI_Farm ai => AIFarmArgs.Create(ai),
            AI_Fish ai => AIFishArgs.Create(ai),
            AI_PlayMusic ai => AIPlayMusicArgs.Create(ai),
            // default
            _ => FakeTask.Default,
        };

        connection.Delta.AddRemote(new CharaTaskDelta {
            Owner = __instance,
            TaskArgs = args,
        });
    }
}