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

        // we are host, relay the client tick to other players
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        // TODO need to buffer this so we don't go insanely fast
        //chara.Stub_Tick();
    }
}