using System.Collections.Generic;
using ElinTogether.Elements;
using ElinTogether.Helper;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaProgressCompleteDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required int CompletedActId { get; init; }

    [Key(2)]
    public required List<ElinDeltaBase> DeltaList { get; init; }

    public static CharaProgressCompleteDelta? Current { get; private set; }

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

        if (!ai.IsChildRunning) {
            EmpLogger.Debug("CharaProgressCompleteDelta: child not running");
        }

        Current = this;

        ai.child.OnProgressComplete();
        ai.child.Success();

        if (ai is Task) {
            ai.Success();
        }

        CharaPickThingDelta.CanApplyOnPC = true;
        DeltaList.ForEach(action => action.Apply(net));
        CharaPickThingDelta.CanApplyOnPC = false;

        Current = null;

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

    public Thing? TryGetProduct()
    {
        for (var i = 0; i < DeltaList.Count; i++) {
            if (DeltaList[i] is ThingDelta { Valid: true } delta) {
                delta.Valid = false;
                return delta.Thing?.Find() as Thing;
            }
        }

        return null;
    }
}