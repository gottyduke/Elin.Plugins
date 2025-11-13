using Cwl.Helper.Exceptions;
using ElinTogether.Helper;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaUseAbilityDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }
    [Key(1)]
    public required int ActId { get; init; }
    [Key(2)]
    public required RemoteCard? TargetCard { get; init; }
    [Key(3)]
    public required Position? Pos { get; init; }
    [Key(4)]
    public required bool Party { get; init; }

    public override void Apply(ElinNetBase net)
    {
        // we do not apply to ourselves
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        var act = chara.elements.GetElement(ActId)?.act ?? ACT.Create(ActId);
        chara.Stub_UseAbility(act, TargetCard, Pos, Party);
    }
}