using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AICraftSnowmanArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    public static AICraftSnowmanArgs Create(AI_Craft_Snowman ai)
    {
        return new() {
            Pos = ai.pos,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Craft_Snowman {
            pos = Pos,
        };
    }
}