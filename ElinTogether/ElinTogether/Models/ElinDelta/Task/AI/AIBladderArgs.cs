using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIBladderArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard ToiletOwner { get; init; }

    public static AIBladderArgs Create(AI_Bladder ai)
    {
        return new() {
            ToiletOwner = ai.toilet.owner,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_Bladder {
            id = ABILITY.AI_Bladder,
            toilet = ToiletOwner.Find()?.trait as TraitBath,
        };
    }
}