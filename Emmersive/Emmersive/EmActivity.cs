using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Emmersive;

public record EmActivity : IDisposable
{
    public static readonly List<EmActivity> Session = [];
    private readonly Stopwatch _sw;

    public readonly Dictionary<string, object> Data = [];

    private EmActivity()
    {
        _sw = Stopwatch.StartNew();
    }

    public static EmActivity? Current { get; private set; }

    public DateTime RequestTime { get; private init; }
    public DateTime EndTime { get; private set; }
    public int InputToken { get; set; }
    public int OutputToken { get; set; }
    public TimeSpan Latency { get; set; } = TimeSpan.Zero;
    public required string ServiceName { get; init; }

    public void Dispose()
    {
        if (Current != this) {
            Current?.Dispose();
        }

        EndTime = DateTime.Now;

        if (Latency == TimeSpan.Zero) {
            Latency = _sw.Elapsed;
        }

        Session.Add(this);

        Current = null;

        EmMod.Debug<EmActivity>(
            $"[{ServiceName}] {RequestTime:hh:mm:ss} {Latency.TotalMilliseconds}ms {InputToken + OutputToken}");
    }

    public static EmActivity StartNew(string serviceId)
    {
        return Current = new() {
            ServiceName = serviceId,
            RequestTime = DateTime.Now,
        };
    }
}