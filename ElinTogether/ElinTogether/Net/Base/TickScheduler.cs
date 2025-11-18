using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElinTogether.Net;

public class TickScheduler
{
    private readonly Queue<TickSubscription> _deferredAdd = [];
    private readonly Queue<Action> _deferredRemove = [];
    private readonly List<TickSubscription> _subscriptions = [];

    public bool IsTicking { get; private set; }

    public void Tick()
    {
        IsTicking = true;
        var now = Time.time;

        foreach (var sub in _subscriptions.Where(sub => now >= sub.NextInvokeTime)) {
            sub.NextInvokeTime += sub.TickInterval;

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

    public void Subscribe(Action onUpdate, float hz)
    {
        if (hz <= 0f) {
            throw new ArgumentOutOfRangeException(nameof(hz));
        }

        var interval = 1f / hz;
        _deferredAdd.Enqueue(new(onUpdate, interval) {
            NextInvokeTime = Time.time + interval,
        });
    }

    public void Unsubscribe(Action onUpdate)
    {
        _deferredRemove.Enqueue(onUpdate);
    }

    private sealed record TickSubscription(Action Update, float TickInterval)
    {
        public float NextInvokeTime;
    }
}