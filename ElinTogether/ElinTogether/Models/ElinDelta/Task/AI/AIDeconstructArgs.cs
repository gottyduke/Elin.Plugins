using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIDeconstructArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIDeconstructArgs Create(AI_Deconstruct ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Deconstruct {
            target = Target,
        };
    }
}