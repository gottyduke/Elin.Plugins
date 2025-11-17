using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIFarmArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static AIFarmArgs Create(AI_Farm ai)
    {
        return new() {
            Pos = ai.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Farm {
            pos = Pos,
        };
    }
}