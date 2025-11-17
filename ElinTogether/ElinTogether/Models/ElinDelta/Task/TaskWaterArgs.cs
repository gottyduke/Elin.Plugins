using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskWaterArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Dest { get; init; }

    public static TaskWaterArgs Create(TaskWater task)
    {
        return new() {
            Dest = task.dest,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskWater {
            dest = Dest,
        };
    }
}