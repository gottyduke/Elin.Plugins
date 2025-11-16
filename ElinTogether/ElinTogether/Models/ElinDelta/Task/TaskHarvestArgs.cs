using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskHarvestArgs : TaskArgsBase
{
    [Key(1)]
    public required Position Pos { get; init; }

    [Key(2)]
    public required BaseTaskHarvest.HarvestType Mode { get; init; }

    [Key(3)]
    public required RemoteCard Target { get; init; }

    public static TaskHarvestArgs Create(TaskHarvest task)
    {
        return new() {
            ActId = ABILITY.TaskHarvest,
            Pos = task.pos,
            Mode = task.mode,
            Target = task.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskHarvest {
            id = ABILITY.TaskHarvest,
            pos = Pos,
            mode = Mode,
            target = Target,
        };
    }
}