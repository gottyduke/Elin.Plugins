using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaMakeAllyDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public bool ShowMsg { get; init; }

    [Key(2)]
    public required string? TemporaryAllyName { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (net.IsHost) {
            // reject every single chara make ally delta from clients
            return;
        }

        if (game?.activeZone?.map is null) {
            net.Delta.DeferLocal(this);
            return;
        }

        if (Owner.Find() is not Chara chara || chara.IsPC) {
            return;
        }

        chara.c_altName = TemporaryAllyName;
        chara.Stub_MakeAlly(ShowMsg);
    }
}