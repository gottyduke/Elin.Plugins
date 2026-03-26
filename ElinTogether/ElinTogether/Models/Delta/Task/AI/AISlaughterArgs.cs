using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AISlaughterArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AISlaughterArgs Create(AI_Slaughter ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Slaughter {
            target = Target,
        };
    }
}