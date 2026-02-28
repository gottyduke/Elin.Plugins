using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaTickDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        // do not remote tick a client
        if (chara.IsPC) {
            return;
        }

        // we are host, relay the client tick to other players
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        // do a remote tick
        chara.Stub_Tick();
    }
}