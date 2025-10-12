using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Emmersive;

public record EmActivity : IDisposable
{
    public static readonly List<EmActivity> Session = [];
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    private EmActivity()
    {
    }

    public static EmActivity? Current { get; private set; }

    public TimeSpan Latency { get; private set; }
    public int Token { get; set; }
    public DateTime RequestTime { get; private init; }
    public required string ServiceId { get; init; }

    public void Dispose()
    {
        if (Current is null) {
            return;
        }

        Latency = _sw.Elapsed;
        Session.Add(this);

        Current = null;

        EmMod.Debug<EmActivity>($"[{ServiceId}] {RequestTime:hh:mm:ss} - {Latency.TotalMilliseconds}ms - {Token}");
    }

    public static EmActivity StartNew(string serviceId)
    {
        return Current = new() {
            ServiceId = serviceId,
            RequestTime = DateTime.Now,
        };
    }
}