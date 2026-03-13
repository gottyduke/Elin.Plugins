using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIEquipArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    public static AIEquipArgs Create(AI_Equip ai)
    {
        return new() {
            Target = ai.target,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Equip {
            target = Target,
        };
    }
}