using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class GameTimeDelta : ElinDeltaBase
{
    [Key(0)]
    public int AdvanceMin { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (game?.world?.date is not { } date) {
            // simply drop delta and wait for reconciliation
            return;
        }

        date.Stub_AdvanceMin(AdvanceMin);
    }
}