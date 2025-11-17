using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaTaskDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required TaskArgsBase? TaskArgs { get; init; }

    [Key(2)]
    public bool Complete { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        // relay to clients
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        if (chara.ai is not GoalRemote remote) {
            return;
        }

        // complete remote tasks because we assigned them max value to prevent randomness
        if (Complete) {
            remote.child?.OnProgressComplete();
            if (chara.NetProfile.CurrentTask.TryGetTarget(out var progress)) {
                progress.OnProgressComplete();
            }
        }

        // now assign new task or reset
        remote.InsertAction(TaskArgs?.CreateSubAct());
    }
}