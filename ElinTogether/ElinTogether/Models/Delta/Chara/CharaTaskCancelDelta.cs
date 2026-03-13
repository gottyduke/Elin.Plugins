using ElinTogether.Helper;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaTaskCancelDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required int ActId { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        var type = SourceValidation.IdToActMapping[ActId];
        var ai = chara.ai.Current;
        while (ai is not null && ai.GetType() != type) {
            ai = ai.parent;
        }

        if (ai is not { status: AIAct.Status.Running }) {
            return;
        }

        // relay to clients
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        ai.Stub_Cancel();
    }
}