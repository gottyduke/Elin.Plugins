using System.Linq;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class EnemyVisibilityDelta : ElinDelta
{
    [Key(0)]
    public required int PlayerId { get; init; }

    [Key(1)]
    public required bool Visible { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        ActionModeCombat.EnemyVisibility[PlayerId] = Visible;
        if (net.IsHost) {
            var visible = ActionModeCombat.EnemyVisibility.Values.Any(v => v);
            net.Delta.AddRemote(new EnemyVisibilityDelta {
                PlayerId = pc.uid,
                Visible = visible,
            });
        } else {
            if (CharaVisibilityChangeEvent.HasEnemyInSight()) {
                net.Delta.AddRemote(new EnemyVisibilityDelta {
                    PlayerId = pc.uid,
                    Visible = false,
                });
            }
        }
    }
}