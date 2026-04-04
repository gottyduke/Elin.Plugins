using MessagePack;

namespace ElinTogether.Models.AI;

[MessagePackObject]
public class AIShearArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIShearArgs Create(AI_Shear ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Shear {
            target = Target,
        };
    }
}