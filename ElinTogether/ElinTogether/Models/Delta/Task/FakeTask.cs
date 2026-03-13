using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class FakeTask : TaskArgsBase
{
    public static FakeTask Default => field ??= new();

    public override AIAct CreateSubAct()
    {
        return new NoGoal();
    }
}