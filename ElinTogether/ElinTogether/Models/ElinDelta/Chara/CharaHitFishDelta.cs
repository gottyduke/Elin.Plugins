using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaHitFishDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (net.IsHost) {
            return;
        }

        if (Owner.Find() is not Chara chara) {
            return;
        }

        if (chara.ai.Current is AI_Fish.ProgressFish fish) {
            fish.hit = 0;
        }
    }
}