using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AICookArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Factory { get; init; }

    public static AICookArgs Create(AI_Cook ai)
    {
        return new() {
            Factory = ai.factory,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Cook {
            factory = Factory,
        };
    }
}