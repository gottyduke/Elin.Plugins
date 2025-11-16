using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
[Union(0, typeof(TaskHarvestArgs))]
public abstract class TaskArgsBase
{
    [Key(0)]
    public required int ActId { get; init; }

    public abstract AIAct CreateSubAct();
}