using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIDanceArgs : TaskArgsBase
{
    public static AIDanceArgs Create(AI_Dance ai)
    {
        return new();
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Dance();
    }
}