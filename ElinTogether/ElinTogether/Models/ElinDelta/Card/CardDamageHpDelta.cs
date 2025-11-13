using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardDamageHpDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required long Dmg { get; init; }

    [Key(2)]
    public required int Ele { get; init; }

    [Key(3)]
    public int EleP { get; init; }

    [Key(4)]
    public AttackSource AttackSource { get; init; }

    [Key(5)]
    public RemoteCard? Origin { get; init; }

    [Key(6)]
    public bool ShowEffect { get; init; }

    [Key(7)]
    public RemoteCard? Weapon { get; init; }

    [Key(8)]
    public RemoteCard? OriginalTarget { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not { } card) {
            return;
        }

        card.Stub_DamageHP(Dmg, Ele, EleP, AttackSource, Origin, ShowEffect, Weapon, OriginalTarget);
    }
}