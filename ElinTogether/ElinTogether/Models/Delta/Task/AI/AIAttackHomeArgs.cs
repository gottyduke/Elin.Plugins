using MessagePack;

namespace ElinTogether.Models.AI;

[MessagePackObject]
public class AIAttackHomeArgs : TaskArgsBase
{
    public static AIAttackHomeArgs Create(AI_AttackHome ai)
    {
        return new();
    }

    public override AIAct CreateSubAct()
    {
        return new AI_AttackHome();
    }
}