using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AICraftArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static AICraftArgs Create(AI_Craft ai)
    {
        return new() {
            Pos = ai.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Craft {
            pos = Pos,
        };
    }
}