using System;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

public class DynamicDelta : ElinDeltaBase
{
    [IgnoreMember]
    public required Action<ElinNetBase> Action { get; init; }

    public override void Apply(ElinNetBase net)
    {
        Action.Invoke(net);
    }
}