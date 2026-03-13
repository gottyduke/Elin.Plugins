using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskDigArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    [Key(1)]
    public required TaskDig.Mode Mode { get; init; }

    public static TaskDigArgs Create(TaskDig task)
    {
        return new() {
            Pos = task.pos,
            Mode = task.mode,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskDig {
            id = ABILITY.TaskDig,
            pos = Pos,
            mode = Mode,
        };
    }
}