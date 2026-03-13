using ElinTogether.Elements;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaTaskDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required TaskArgsBase? TaskArgs { get; init; }

    protected override void OnApply(ElinNetBase net)
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

        // now assign new task or reset
        remote.InsertAction(TaskArgs?.CreateSubAct());
    }
}