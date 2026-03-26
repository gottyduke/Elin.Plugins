using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIStealArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIStealArgs Create(AI_Steal ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Steal {
            target = Target,
        };
    }
}