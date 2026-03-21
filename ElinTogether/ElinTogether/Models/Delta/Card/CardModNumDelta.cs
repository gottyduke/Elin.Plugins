using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardModNumDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    [Key(1)]
    public required int Num { get; init; }

    public static new bool IsApplying { get; private set; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Card.Find() is not { isDestroyed: false } card) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        IsApplying = true;
        card.SetNum(Num);
        IsApplying = false;
    }
}