using System.Linq;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskCullLifeArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Dest { get; init; }

    [Key(1)]
    public required RemoteCard[] ListSkip { get; init; }

    public static TaskCullLifeArgs Create(TaskCullLife task)
    {
        return new() {
            Dest = task.dest,
            ListSkip = task.listSkip
                .Select(c => RemoteCard.Create(c))
                .ToArray(),
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskCullLife {
            dest = Dest,
            listSkip = ListSkip
                .Select(c => c.Find() as Chara)
                .OfType<Chara>()
                .ToHashSet(),
        };
    }
}