using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaPickThingDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required RemoteCard Thing { get; init; }

    public override void Apply(ElinNetBase net)
    {
        // we do not apply to ourselves
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        if (Thing.Find() is not Thing thing) {
            return;
        }

        chara.Pick(thing);

        // the patch should handle the relay automatically
        // albeit a bad code design to separate it
    }
}