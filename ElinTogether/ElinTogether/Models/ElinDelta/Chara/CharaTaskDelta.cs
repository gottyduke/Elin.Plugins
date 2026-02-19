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
    public int CompletedActId { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        // complete remote tasks because we assigned them max value to prevent randomness
        if (CompletedActId != 0) {
            var type = SourceValidation.IdToActMapping[CompletedActId];
            var ai = chara.ai.Current;
            while (ai is not null && ai.GetType() != type) {
                ai = ai.parent;
            }

            if (ai is null) {
                return;
            }

            ai.OnProgressComplete();
            if (ai is AIProgress) {
                ai = ai.parent;
            }
            ai.Success();
        }

        if (chara.IsPC) {
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