using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ElinTogether.Elements;

internal class GoalRemote : NoGoal
{
    [field: AllowNull]
    internal static GoalRemote Default => field ??= new();

    public override bool IsIdle => true;

    public override bool IsNoGoal => true;

    public override int MaxRestart => 114514 ^ 1919810;

    public override bool CancelWhenDamaged => false;

    public override bool PushChara => false;

    public override IEnumerable<Status> Run()
    {
        while (!owner.isDestroyed) {
            yield return Status.Running;
        }
    }
}