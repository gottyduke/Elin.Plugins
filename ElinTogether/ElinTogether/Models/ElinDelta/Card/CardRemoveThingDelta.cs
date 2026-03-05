using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardRemoveThingDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Thing { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Thing.Find() is not Thing thing) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        thing.parent?.RemoveCard(thing);

        CardCache.KeepAlive(thing);
    }
}