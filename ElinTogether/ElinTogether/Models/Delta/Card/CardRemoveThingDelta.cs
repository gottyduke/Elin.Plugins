using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CardRemoveThingDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Thing { get; init; }

    protected override void OnApply(ElinNetBase net)
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