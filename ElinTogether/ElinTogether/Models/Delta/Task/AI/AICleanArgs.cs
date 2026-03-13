using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AICleanArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static AICleanArgs Create(AI_Clean ai)
    {
        return new() {
            Pos = ai.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Clean {
            pos = Pos,
        };
    }
}