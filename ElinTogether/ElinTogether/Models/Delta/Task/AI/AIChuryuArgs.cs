using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIChuryuArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Churyu { get; init; }

    [Key(1)]
    public required RemoteCard Slave { get; init; }

    public static AIChuryuArgs Create(AI_Churyu ai)
    {
        return new() {
            Churyu = ai.churyu,
            Slave = ai.slave,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Churyu {
            churyu = Churyu,
            slave = Slave,
        };
    }
}