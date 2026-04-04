using MessagePack;

namespace ElinTogether.Models;

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