using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaRemoveFromGameDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    public override void Apply(ElinNetBase net)
    {
        // this is a client operation
        if (net.IsHost) {
            return;
        }

        if (Owner.Find() is not Chara chara) {
            return;
        }

        pc.party.RemoveMember(chara);
        game.cards.globalCharas.Remove(chara);
        _zone.RemoveCard(chara);
    }
}