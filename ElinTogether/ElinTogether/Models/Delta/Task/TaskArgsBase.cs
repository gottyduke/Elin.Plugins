using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
[Union(0, typeof(NoTask))]
[Union(1, typeof(FakeTask))]
// Tasks
[Union(100, typeof(TaskCleanArgs))]
[Union(101, typeof(TaskCullLifeArgs))]
[Union(102, typeof(TaskCutArgs))]
[Union(103, typeof(TaskDigArgs))]
[Union(104, typeof(TaskDrawWaterArgs))]
[Union(105, typeof(TaskDumpArgs))]
[Union(106, typeof(TaskHarvestArgs))]
[Union(107, typeof(TaskMineArgs))]
[Union(108, typeof(TaskPlowArgs))]
[Union(109, typeof(TaskPourWaterArgs))]
[Union(110, typeof(TaskWaterArgs))]
// AI Tasks
[Union(200, typeof(AIArmPillowArgs))]
[Union(201, typeof(AIAttackHomeArgs))]
[Union(202, typeof(AIBladderArgs))]
[Union(203, typeof(AIChuryuArgs))]
[Union(204, typeof(AICleanArgs))]
[Union(205, typeof(AICookArgs))]
[Union(206, typeof(AICraftArgs))]
[Union(207, typeof(AICraftSnowmanArgs))]
[Union(208, typeof(AIDanceArgs))]
[Union(209, typeof(AIDeconstructArgs))]
[Union(210, typeof(AIDrinkArgs))]
[Union(211, typeof(AIEatArgs))]
[Union(212, typeof(AIEquipArgs))]
[Union(213, typeof(AIFarmArgs))]
[Union(214, typeof(AIFishArgs))]
[Union(215, typeof(AIPlayMusicArgs))]
public abstract class TaskArgsBase
{
    public abstract AIAct CreateSubAct();
}