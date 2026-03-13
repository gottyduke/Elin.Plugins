using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskPlowArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static TaskPlowArgs Create(TaskPlow task)
    {
        return new() {
            Pos = task.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskPlow {
            id = ABILITY.TaskPlow,
            pos = Pos,
        };
    }
}