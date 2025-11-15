using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class ZoneAddCardDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    [Key(1)]
    public required int ZoneUid { get; init; }

    [Key(2)]
    public required Position Pos { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (game?.activeZone?.map is null) {
            net.Delta.DeferLocal(this);
            return;
        }

        var zone = game.spatials.Find(ZoneUid) ?? SpatialGenEvent.TryPop(ZoneUid);
        if (zone is null) {
            return;
        }

        var card = Card.Find();
        if (card is null) {
            return;
        }

        // relay to other clients
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        // do not add client characters from this delta
        if (card.IsPC) {
            return;
        }

        // do not update it again if same position
        // we derive all other state changes to sub deltas
        if (card.pos != Pos) {
            zone.Stub_AddCard(card, Pos.X, Pos.Z);
        }
    }
}