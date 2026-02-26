using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaProgressBeginDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required Position Pos { get; init; }

    [Key(2)]
    public required int ActId { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        if (chara.ai is not GoalRemote remote) {
            return;
        }

        var type = SourceValidation.IdToActMapping[ActId];
        var ai = remote.Current;
        while (ai is not null && ai.GetType() != type) {
            ai = ai.parent;
        }

        if (ai is null) {
            return;
        }

        // advance to create progress
        chara.Stub_Move(Pos, Card.MoveType.Force);
        while (ai.child is not AIProgress { status: AIAct.Status.Running }) {
            ai.Tick();
        }

        var child = ai.child as AIProgress;
        child!.progress = 1;

        // relay to clients
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }
    }
}