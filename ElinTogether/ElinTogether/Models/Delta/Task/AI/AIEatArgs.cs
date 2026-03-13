using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIEatArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    [Key(1)]
    public required bool Cook { get; init; }

    public static AIEatArgs Create(AI_Eat ai)
    {
        return new() {
            Target = ai.target,
            Cook = ai.cook,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Eat {
            target = Target,
            cook = Cook,
        };
    }
}