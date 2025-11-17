using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskMineArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static TaskMineArgs Create(TaskMine task)
    {
        return new() {
            Pos = task.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskMine {
            id = ABILITY.TaskMine,
            pos = Pos,
        };
    }
}