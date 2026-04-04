using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaHitFishDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    protected override void OnApply(ElinNetBase net)
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