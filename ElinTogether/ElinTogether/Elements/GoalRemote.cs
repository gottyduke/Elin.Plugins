using System.Collections.Generic;

namespace ElinTogether.Elements;

internal class GoalRemote : NoGoal
{
    internal static GoalRemote Default => new();

    public override bool IsIdle => true;

    public override bool IsNoGoal => true;

    public override int MaxRestart => 114_514 & 19_19_810;

    public override bool CancelWhenDamaged => false;

    public override bool PushChara => false;

    public override IEnumerable<Status> Run()
    {
        while (!owner.isDestroyed) {
            yield return Status.Running;
        }
    }

    public void InsertAction(AIAct? action)
    {
        HaltChildAct();

        if (action is null) {
            return;
        }

        if (action is TaskMine t) {
            t.SetTarget(owner);
        }

        child = action;
        child.SetOwner(owner);

        Tick();
    }

    public void HaltChildAct()
    {
        child?.Reset();
        child = null;
    }
}