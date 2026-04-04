using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class GameDelta : ElinDelta
{
    [Key(0)]
    public required float Delta { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        Synchronization.GameDelta += Delta;
    }
}