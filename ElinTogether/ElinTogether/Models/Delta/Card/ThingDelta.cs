using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ThingDelta : ElinDelta
{
    [IgnoreMember]
    public bool Valid = true;

    [Key(0)]
    public required RemoteCard? Thing { get; init; }

    protected override void OnApply(ElinNetBase net) { }
}