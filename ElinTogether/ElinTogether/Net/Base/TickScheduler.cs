using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElinTogether.Net;

public class TickScheduler
{
    private readonly Queue<TickSubscription> _deferredAdd = [];
    private readonly Queue<Action> _deferredRemove = [];
    private readonly List<TickSubscription> _subscriptions = [];
    private static int BaseTickRate => Mathf.RoundToInt(1f / Time.fixedDeltaTime);

    public bool IsTicking { get; private set; }

    public void Tick()
    {
        IsTicking = true;

        foreach (var sub in _subscriptions) {
            sub.TickCounter++;

            if (sub.TickCounter < sub.TicksPerInvoke) {
                continue;
            }

            sub.TickCounter = 0;

            try {
                sub.Update();
            } catch (Exception ex) {
                EmpLog.Debug(ex, "Exception at tick update {TickHandler}",
                    sub.Update.Method.Name);
                // noexcept
            }
        }

        IsTicking = false;

        // must remove first, due to failsafe logic in other parts
        while (_deferredRemove.TryDequeue(out var handler)) {
            _subscriptions.RemoveAll(s => s.Update == handler);
        }

        while (_deferredAdd.TryDequeue(out var handler)) {
            _subscriptions.Add(handler);
        }
    }

    public void Subscribe(Action onUpdate, int tickPerSecond)
    {
        if (tickPerSecond <= 0) {
            throw new ArgumentOutOfRangeException(nameof(tickPerSecond));
        }

        var ticksPerInvoke = Mathf.Max(1, BaseTickRate / tickPerSecond);
        EmpLog.Verbose("Added new tick handler {TickHandler} at {Interval}Hz (every {Ticks} ticks)",
            onUpdate.Method.Name, tickPerSecond, ticksPerInvoke);

        _deferredAdd.Enqueue(new(onUpdate, ticksPerInvoke));
    }

    public void Unsubscribe(Action onUpdate)
    {
        _deferredRemove.Enqueue(onUpdate);
    }

    private sealed record TickSubscription(Action Update, int TicksPerInvoke)
    {
        public int TickCounter { get; set; }
    }
}