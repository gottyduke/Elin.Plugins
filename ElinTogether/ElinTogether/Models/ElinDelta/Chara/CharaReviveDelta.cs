using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaReviveDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        chara.Stub_Revive(msg: true);

        if (net is ElinNetHost host) {
            host.Delta.AddRemote(this);
        }

        // add back to party
        if (chara is { c_wasInPcParty: true, IsPCParty: false }) {
            pc.party.AddMemeber(chara);
        }
    }
}