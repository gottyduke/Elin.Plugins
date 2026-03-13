using System;
using ElinTogether.Net;

namespace ElinTogether.Models.ElinDelta;

public class DynamicDelta : ElinDelta
{
    public required Action<ElinNetBase> Action { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        Action.Invoke(net);
    }
}