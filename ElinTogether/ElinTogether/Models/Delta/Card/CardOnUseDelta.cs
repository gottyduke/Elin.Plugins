using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardOnUseDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    [Key(1)]
    public required RemoteCard? RootCard { get; init; }

    [Key(2)]
    public required RemoteCard User { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Card.Find() is not { isDestroyed: false } card
            || User.Find() is not Chara user) {
            return;
        }

        if (RootCard?.Find() != card.GetRootCard()) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        card.trait.OnUse(user);
    }
}