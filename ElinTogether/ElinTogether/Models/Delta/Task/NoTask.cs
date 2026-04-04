using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class NoTask : TaskArgsBase
{
    public static NoTask Default => field ??= new();

    public override AIAct CreateSubAct()
    {
        return null!;
    }
}