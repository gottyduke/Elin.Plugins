using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaReviveDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required string? LastWords { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        chara.Revive(msg: true);

        if (net is ElinNetHost host) {
            host.Delta.AddRemote(this);
            chara.MakeGrave(LastWords);
        }

        // add back to party
        if (chara is { c_wasInPcParty: true, IsPCParty: false }) {
            pc.party.AddMemeber(chara);
        }
    }
}