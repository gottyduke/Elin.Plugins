using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaDieDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public int? ElementId { get; init; }

    [Key(2)]
    public RemoteCard? Origin { get; init; }

    [Key(3)]
    public AttackSource AttackSource { get; set; } = AttackSource.None;

    [Key(4)]
    public RemoteCard? OriginalTarget { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (net.IsHost) {
            // reject every single chara die delta from clients
            return;
        }

        if (Owner.Find() is not Chara chara) {
            return;
        }

        var element = ElementId is null ? null : Element.Create(ElementId.Value);
        chara.Stub_Die(element, Origin, AttackSource, OriginalTarget);
    }
}