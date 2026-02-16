using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class GameDelta : ElinDeltaBase
{
    [Key(0)]
    public required float Delta { get; init; }

    public override void Apply(ElinNetBase net)
    {
        Synchronization.GameDelta += Delta;
    }
}