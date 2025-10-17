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
        Timeout,
    }

    public static readonly List<EmActivity> Session = [];
    private static readonly HashSet<string> _services = [];
    private static readonly List<EmActivity> _unhandled = [];

    private readonly Stopwatch _sw;

    private EmActivity()
    {
        _sw = Stopwatch.StartNew();
        ActivityId = InternalCount;
    }

    private static int InternalCount => ++field;

    public static EmActivity? Current { get; private set; }

    public static int Unhandled
    {
        get {
            lock (_unhandled) {
                return _unhandled.Count;
            }
        }
    }

    public DateTime RequestTime { get; private init; }
    public DateTime EndTime { get; private set; }
    public int InputToken { get; set; }
    public int OutputToken { get; set; }
    public TimeSpan Latency { get; set; } = TimeSpan.Zero;
    public StatusType Status { get; private set; } = StatusType.Unknown;

    public required string ServiceName { get; init; }
    public int ActivityId { get; }

    public void Dispose()
    {
        EndTime = DateTime.Now;

        Latency = _sw.Elapsed;

        lock (_unhandled) {
            _unhandled.RemoveAll(a => a == this);
        }

        EmMod.Log<EmActivity>(
            $"<{ActivityId}> [{ServiceName}] " +
            $"{RequestTime:HH:mm:ss} stopped " +
            $"{Latency.TotalMilliseconds}ms " +
            $"{InputToken}+{OutputToken}");
    }

    public void SetStatus(StatusType status)
    {
        if (status is not (StatusType.Failed or StatusType.Timeout)) {
            ThrowIfTimeout();
        }

        Status = status;
    }

    public void ThrowIfTimeout()
    {
        if (Status == StatusType.Timeout) {
            throw new OperationCanceledException();
        }
    }

    public static EmActivity StartNew(string serviceId)
    {
        _services.Add(serviceId);

        var activity = new EmActivity {
            ServiceName = serviceId,
            RequestTime = DateTime.Now,
        };

        lock (_unhandled) {
            _unhandled.Add(activity);
        }

        Session.Add(activity);

        EmMod.Log<EmActivity>(
            $"<{activity.ActivityId}> [{activity.ServiceName}] " +
            $"{activity.RequestTime:HH:mm:ss} started");

        return Current = activity;
    }

    public static IEnumerable<EmActivity> FromProvider(string serviceName)
    {
        return Session.Where(a => a.ServiceName == serviceName);
    }

    public static EmActivity? FromProviderLatest(string serviceName)
    {
        return Session.LastOrDefault(a => a.ServiceName == serviceName);
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

            if (a.Status == StatusType.Timeout) {
                continue;
            }

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