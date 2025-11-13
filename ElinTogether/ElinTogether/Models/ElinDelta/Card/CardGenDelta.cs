using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardGenDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Card.Data is null) {
            return;
        }

        Card card = Card.Type == RemoteCard.CardType.Thing
            ? Card.Data.Decompress<Thing>()
            : Card.Data.Decompress<Chara>();

        card.uid = Card.Uid;

        CardGenEvent.HeldRefCards[Card.Uid] = card;
    }
}