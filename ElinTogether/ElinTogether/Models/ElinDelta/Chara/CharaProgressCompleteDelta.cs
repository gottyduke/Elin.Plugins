using System.Collections.Generic;
using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Net;
using MessagePack;
using UnityEngine.Assertions;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaProgressCompleteDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public int CompletedActId { get; init; }

    [Key(2)]
    public required List<CharaPickThingDelta> Actions { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        // complete remote tasks because we assigned them max value to prevent randomness
        var type = SourceValidation.IdToActMapping[CompletedActId];
        var ai = chara.ai.Current;
        while (ai is not null && ai.GetType() != type) {
            ai = ai.parent;
        }

        if (ai is null) {
            return;
        }

        Assert.IsTrue(ai.IsChildRunning);
        ai.child.OnProgressComplete();
        ai.Success();

        CharaPickThingDelta.CanApplyOnPC = true;
        Actions.ForEach(action => action.Apply(net));
        CharaPickThingDelta.CanApplyOnPC = false;

        if (chara.IsPC) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        if (chara.ai is not GoalRemote remote) {
            return;
        }

        remote.InsertAction(null);
    }
}