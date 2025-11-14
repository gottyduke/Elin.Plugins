using System.Collections.Generic;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaActPerformDelta : ElinDeltaBase
{
    // builtin acts that are not instantiated
    private static readonly Dictionary<int, Act> _builtInMapping = new() {
        [ABILITY.ActWait] = ACT.Wait,
        [ABILITY.ActChat] = ACT.Chat,
        [ABILITY.ActPick] = ACT.Pick,
        [ABILITY.ActKick] = ACT.Kick,
        [ABILITY.ActMelee] = ACT.Melee,
        [ABILITY.ActRanged] = ACT.Ranged,
        [ABILITY.ActThrow] = ACT.Throw,
        [ABILITY.ActItem] = ACT.Item,
    };

    [Key(0)]
    public required int ActId { get; init; }

    [Key(1)]
    public required RemoteCard Owner { get; init; }

    [Key(2)]
    public required RemoteCard? TargetCard { get; init; }

    [Key(3)]
    public required Position? Pos { get; init; }

    public static CharaActPerformDelta Create(Act act)
    {
        return new() {
            ActId = act.id,
            Owner = Act.CC,
            TargetCard = Act.TC,
            Pos = Act.TP,
        };
    }

    public override void Apply(ElinNetBase net)
    {
        // we do not apply to ourselves
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        var act = _builtInMapping.GetValueOrDefault(ActId, chara.elements.GetElement(ActId)?.act ?? ACT.Create(ActId));
        act.Perform(chara, TargetCard, Pos);
    }
}