using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardModNumDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    [Key(1)]
    public required int Num { get; init; }

    public override void Apply(ElinNetBase net) {
        if (Card.Find() is not { isDestroyed: false } card) {
            return;
        }

        card.SetNum(Num);
    }
}