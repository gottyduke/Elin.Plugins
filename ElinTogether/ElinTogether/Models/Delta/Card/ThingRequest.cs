using System;
using System.Collections.Generic;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class ThingRequest : ElinDelta
{
    [Key(0)]
    public required int Id { get; init; }

    [Key(1)]
    public required RemoteCard? Thing { get; init; }

    [Key(2)]
    public required int Num { get; init; }

    private static readonly Dictionary<int, (Action<Thing>, Action?)> _callbackList = [];

    private static int _nextId = 0;

    protected override void OnApply(ElinNetBase net)
    {
        var thing = Thing?.Find() as Thing;
        if (net.IsClient && _callbackList.TryGetValue(Id, out var value)) {
            var (onSuccess, onFail) = value;
            if (thing is not null) {
                onSuccess(thing);
            } else {
                onFail?.Invoke();
            }

            return;
        }

        if (thing is null || thing.parent is null) {
            Fail();
            return;
        }

        var result = thing.Split(Num);
        result.parent?.RemoveCard(result);
        CardCache.KeepAlive(result);

        Success(result);
    }

    public void Success(Thing thing)
    {
        NetSession.Instance.Connection?.Delta.AddRemote(new ThingRequest {
            Id = Id,
            Thing = thing,
            Num = Num
        });
    }

    public void Fail()
    {
        NetSession.Instance.Connection?.Delta.AddRemote(new ThingRequest {
            Id = Id,
            Thing = null,
            Num = Num
        });
    }

    public static ThingRequest Create(Thing thing, int num)
    {
        var req = new ThingRequest {
            Id = _nextId++,
            Thing = thing,
            Num = num,
        };

        NetSession.Instance.Connection?.Delta.AddRemote(req);
        return req;
    }

    public void Then(Action<Thing> onSuccess, Action? onFail = null)
    {
        _callbackList[Id] = (onSuccess, onFail);
    }
}