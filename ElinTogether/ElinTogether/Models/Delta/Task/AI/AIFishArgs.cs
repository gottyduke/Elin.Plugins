using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIFishArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static AIFishArgs Create(AI_Fish ai)
    {
        return new() {
            Pos = ai.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Fish {
            id = ABILITY.AI_Fish,
            pos = Pos,
        };
    }
}