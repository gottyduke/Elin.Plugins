using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class ThingDelta : ElinDeltaBase
{
    [IgnoreMember]
    public bool Valid = true;

    [Key(0)]
    public required RemoteCard? Thing { get; init; }

    public override void Apply(ElinNetBase net) { }
}