using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaRemoveFromGameDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    protected override void OnApply(ElinNetBase net)
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