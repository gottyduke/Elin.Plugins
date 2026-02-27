using System;
using ElinTogether.Net;

namespace ElinTogether.Models.ElinDelta;

public class DynamicDelta : ElinDeltaBase
{
    public required Action<ElinNetBase> Action { get; init; }

    public override void Apply(ElinNetBase net)
    {
        Action.Invoke(net);
    }
}