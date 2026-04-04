using MessagePack;

namespace ElinTogether.Models.AI;

[MessagePackObject]
public class AIReadArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIReadArgs Create(AI_Read ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Read {
            target = Target,
        };
    }
}