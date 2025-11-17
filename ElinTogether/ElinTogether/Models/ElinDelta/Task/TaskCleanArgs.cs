using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskCleanArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Dest { get; init; }

    public static TaskCleanArgs Create(TaskClean task)
    {
        return new() {
            Dest = task.dest,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskClean {
            dest = Dest,
        };
    }
}