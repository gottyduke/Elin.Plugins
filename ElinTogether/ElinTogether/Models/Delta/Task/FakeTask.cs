using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class FakeTask : TaskArgsBase
{
    public static FakeTask Default => field ??= new();

    public override AIAct CreateSubAct()
    {
        return new NoGoal();
    }
}