using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIDrinkArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIDrinkArgs Create(AI_Drink ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Drink {
            target = Target,
        };
    }
}