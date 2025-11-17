using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class NoTask : TaskArgsBase
{
    public static NoTask Default => field ??= new();

    public override AIAct CreateSubAct()
    {
        return null!;
    }
}