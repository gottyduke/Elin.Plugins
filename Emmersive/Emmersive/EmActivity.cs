using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Emmersive;

public record EmActivity : IDisposable
{
    public enum StatusType
    {
        Unknown,
        InProgress,
        Completed,
        Failed,
    }

    public static readonly List<EmActivity> Session = [];
    private static readonly HashSet<string> _services = [];

    private readonly Stopwatch _sw;

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
    public StatusType Status { get; set; } = StatusType.Unknown;

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

        EmMod.Log<EmActivity>(
            $"[{ServiceName}] {RequestTime:HH:mm:ss} {Latency.TotalMilliseconds}ms {InputToken + OutputToken}");
    }

    public static EmActivity StartNew(string serviceId)
    {
        Current = new() {
            ServiceName = serviceId,
            RequestTime = DateTime.Now,
        };

        _services.Add(serviceId);

        return Current;
    }

    public static IEnumerable<EmActivity> FromProvider(string serviceName)
    {
        return Session.Where(a => a.ServiceName == serviceName);
    }

    public static IEnumerable<EmActivitySummary> GetAllSummaries()
    {
        return _services.Select(GetSummary);
    }

    public static EmActivitySummary GetSummary(string serviceName)
    {
        var activities = FromProvider(serviceName).ToArray();
        var total = activities.Length;

        var success = 0;
        var tokensInput = 0;
        var tokensOutput = 0;
        var totalLatencySec = 0d;
        var totalLatencyMin = 0d;
        var latencyCount = 0;
        var tokensLastHour = 0;
        var requestsLastHour = 0;

        var now = DateTime.Now;
        var oneHourAgo = now - TimeSpan.FromHours(1);

        foreach (var a in activities) {
            if (a.Status == StatusType.Completed) {
                success++;
            }

            tokensInput += a.InputToken;
            tokensOutput += a.OutputToken;

            if (a.Latency > TimeSpan.Zero) {
                totalLatencySec += a.Latency.TotalSeconds;
                totalLatencyMin += a.Latency.TotalMinutes;
                latencyCount++;
            }

            if (a.EndTime > oneHourAgo) {
                tokensLastHour += a.InputToken + a.OutputToken;
                requestsLastHour++;
            }
        }

        return new() {
            ServiceName = serviceName,
            RequestTotal = total,
            RequestSuccess = success,
            TokensInput = tokensInput,
            TokensOutput = tokensOutput,
            TotalLatencyMin = totalLatencyMin,
            TotalLatencySec = totalLatencySec,
            LatencyCount = latencyCount,
            TokensLastHour = tokensLastHour,
            RequestLastHour = requestsLastHour,
        };
    }

    public class EmActivitySummary
    {
        public required string ServiceName { get; init; }

        public int RequestTotal { get; init; }
        public int RequestSuccess { get; init; }
        public int RequestLastHour { get; init; }
        public int RequestFailure => RequestTotal - RequestSuccess;
        public int RequestPerMin => (int)(TotalLatencyMin > 0 ? RequestTotal / TotalLatencyMin : 0);
        public int RequestSuccessPerMin => (int)(TotalLatencyMin > 0 ? RequestSuccess / TotalLatencyMin : 0);

        public long TokensInput { get; init; }
        public long TokensOutput { get; init; }
        public int TokensLastHour { get; init; }
        public long TokensTotal => TokensInput + TokensOutput;
        public double TokensPerMin => TotalLatencyMin > 0 ? TokensTotal / TotalLatencyMin : 0;
        public double TokensPerRequest => RequestTotal > 0 ? (double)TokensTotal / RequestTotal : 0;

        public double TotalLatencySec { get; init; }
        public double TotalLatencyMin { get; init; }
        public int LatencyCount { get; init; }
        public double AverageLatency => LatencyCount > 0 ? TotalLatencySec / LatencyCount : 0;
    }
}