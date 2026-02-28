using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CardAddThingDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Thing { get; init; }

    [Key(1)]
    public required RemoteCard Parent { get; init; }

    [Key(2)]
    public required bool TryStack { get; init; }

    [Key(3)]
    public required int DestInvX { get; init; }

    [Key(4)]
    public required int DestInvY { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Thing.Find() is not Thing thing || Parent?.Find() is not { } parent) {
            return;
        }

        if (thing.parent != parent) {
            parent.Stub_AddThing(thing, TryStack, DestInvX, DestInvY);
        }
    }
}