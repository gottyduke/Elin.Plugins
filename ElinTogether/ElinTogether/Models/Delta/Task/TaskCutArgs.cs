using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskCutArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static TaskCutArgs Create(TaskCut task)
    {
        return new() {
            Pos = task.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskCut {
            id = ABILITY.TaskCut,
            pos = Pos,
        };
    }
}