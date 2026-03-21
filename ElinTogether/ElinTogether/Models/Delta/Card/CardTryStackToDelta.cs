using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardTryStackToDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    [Key(1)]
    public required RemoteCard To { get; init; }

    [Key(2)]
    public required RemoteCard? Parent { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Card.Find() is not { isDestroyed: false } card) {
            return;
        }

        if (To.Find() is not Thing { isDestroyed: false } to) {
            Parent?.Find()?.AddCard(card);
            return;
        }

        if (Parent?.Find() is {} parent && parent != to.parent) {
            parent.AddCard(card);
            return;
        }

        if (Parent is null && to.parent is not Zone) {
            _zone.AddCard(card);
            return;
        }

        card.TryStackTo(to);
    }
}