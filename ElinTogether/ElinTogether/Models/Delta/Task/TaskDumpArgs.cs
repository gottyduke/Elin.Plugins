using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskDumpArgs : TaskArgsBase
{
    public static TaskDumpArgs Create(TaskDump task)
    {
        return new();
    }

    public override AIAct CreateSubAct()
    {
        return new TaskDump();
    }
}