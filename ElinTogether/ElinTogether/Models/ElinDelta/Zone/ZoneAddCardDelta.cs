using ElinTogether.Net;
using ElinTogether.Patches.DeltaEvents;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class ZoneAddCardDelta : IElinDelta
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    [Key(1)]
    public int ZoneUid { get; init; }

    [Key(2)]
    public int PosX { get; init; }

    [Key(3)]
    public int PosZ { get; init; }

    public void Apply(ElinNetBase net)
    {
        if (EClass.game?.activeZone?.map is null) {
            net.Delta.DeferLocal(this);
            return;
        }

        var zone = EClass.game.spatials.Find(ZoneUid);
        if (zone is null) {
            return;
        }

        var card = Card.Find();
        if (card is null) {
            return;
        }

        // do not add clients from this delta
        if (card.IsPC) {
            return;
        }

        zone.Stub_AddCard(card, PosX, PosZ);
    }
}