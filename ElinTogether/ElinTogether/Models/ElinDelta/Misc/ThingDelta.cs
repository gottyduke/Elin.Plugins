using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class ThingDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard? Thing { get; init; }

    public override void Apply(ElinNetBase net) { }
}