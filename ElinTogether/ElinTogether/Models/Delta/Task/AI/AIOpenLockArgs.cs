using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIOpenLockArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIOpenLockArgs Create(AI_OpenLock ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_OpenLock {
            target = Target,
        };
    }
}