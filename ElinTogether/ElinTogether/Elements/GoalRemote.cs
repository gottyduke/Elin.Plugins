using System.Collections.Generic;
using System.Linq;

namespace ElinTogether.Elements;

internal class GoalRemote : AIAct
{
    internal static GoalRemote Default => new();

    public override bool IsIdle => true;

    public override bool IsNoGoal => true;

    public override int MaxRestart => 114_514 & 19_19_810;

    public override bool CancelWhenDamaged => false;

    public override bool PushChara => false;

    // took from AutoAct
    // actually no idea how it works
    public void InsertAction(AIAct action)
    {
        if (Enumerator is null) {
            Tick();
        }

        if (child is null) {
            SetChild(action, KeepRunning);
            return;
        }

        child.SetOwner(owner);

        AIAct last = this;
        while (last.child?.IsRunning is true) {
            last = last.child;
            last.Enumerator = Enumerable.Repeat(Status.Success, 1).GetEnumerator();
        }

        last.Enumerator = OnEnd().GetEnumerator();
        last.SetChild(action, KeepRunning);

        return;

        IEnumerable<Status> OnEnd()
        {
            last.child?.Reset();
            last.child = null;
            yield return Status.Success;
        }
    }
}