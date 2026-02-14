using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardPlacedDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required PlaceState PlaceState { get; init; }

    [Key(2)]
    public required int Dir { get; init; }

    [Key(3)]
    public required bool ByPlayer { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not { } card) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        card.dir = Dir;
        card.Stub_SetPlacedState(PlaceState, ByPlayer);
    }
}