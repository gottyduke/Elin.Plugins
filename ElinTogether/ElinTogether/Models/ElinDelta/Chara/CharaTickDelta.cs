using ElinTogether.Net;
using ElinTogether.Patches.DeltaEvents;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaTickDelta : IElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    public void Apply(ElinNetBase net)
    {
        // we do not apply to ourselves
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        // we are host, relay the client tick to other players
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        chara.Stub_Tick();
    }
}