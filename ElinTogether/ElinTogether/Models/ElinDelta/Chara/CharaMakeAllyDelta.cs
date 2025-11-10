using ElinTogether.Net;
using ElinTogether.Patches.DeltaEvents;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaMakeAllyDelta : IElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public bool ShowMsg { get; init; }

    public void Apply(ElinNetBase net)
    {
        if (EClass.game?.activeZone?.map is null) {
            net.Delta.DeferLocal(this);
            return;
        }

        if (Owner.Find() is not Chara chara || chara.IsPC) {
            return;
        }

        chara.Stub_MakeAlly(ShowMsg);
    }
}