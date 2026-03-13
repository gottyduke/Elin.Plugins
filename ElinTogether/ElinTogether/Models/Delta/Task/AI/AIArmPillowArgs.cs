using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIArmPillowArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIArmPillowArgs Create(AI_ArmPillow ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_ArmPillow {
            target = Target,
        };
    }
}