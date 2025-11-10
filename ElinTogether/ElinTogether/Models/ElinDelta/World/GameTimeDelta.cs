using ElinTogether.Net;
using ElinTogether.Patches.DeltaEvents;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class GameTimeDelta : IElinDelta
{
    [Key(0)]
    public int AdvanceMin { get; init; }

    public void Apply(ElinNetBase net)
    {
        if (EClass.game?.world?.date is not { } date) {
            // simply drop delta and wait for reconciliation
            return;
        }

        date.Stub_AdvanceMin(AdvanceMin);
    }
}