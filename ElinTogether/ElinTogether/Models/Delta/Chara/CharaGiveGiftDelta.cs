using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaGiveGiftDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard From { get; init; }

    [Key(1)]
    public required RemoteCard To { get; init; }

    [Key(2)]
    public required RemoteCard Thing { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (From.Find() is not Chara from || To.Find() is not Chara to || Thing.Find() is not Thing thing) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        from.Stub_GiveGift(to, thing);
    }
}