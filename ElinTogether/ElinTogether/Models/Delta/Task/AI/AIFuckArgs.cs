using MessagePack;

namespace ElinTogether.Models.AI;

[MessagePackObject]
public class AIFuckArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Target { get; init; }

    [Key(1)]
    public required AI_Fuck.FuckType Type { get; init; }

    public static AIFuckArgs Create(AI_Fuck ai)
    {
        return new() {
            Target = ai.target,
            Type = ai.Type,
        };
    }

    public override AIAct CreateSubAct()
    {
        if (Type == AI_Fuck.FuckType.fuck) {
            return new AI_Fuck {
                target = Target,
            };
        }

        return new AI_TendAnimal {
            target = Target,
        };
    }
}