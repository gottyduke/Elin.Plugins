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

    private readonly Stopwatch _sw;

    private EmActivity()
    {
        _sw = Stopwatch.StartNew();
        ActivityId = InternalCount;
    }

    private static int InternalCount => ++field;

    public static EmActivity? Current { get; private set; }

    public DateTime RequestTime { get; private init; }
    public DateTime EndTime { get; private set; }
    public int TokensInput { get; set; }
    public int TokensOutput { get; set; }
    public TimeSpan Latency { get; set; } = TimeSpan.Zero;
    public StatusType Status { get; private set; } = StatusType.Unknown;

    public required string ServiceName { get; init; }
    public int ActivityId { get; }

    public void Dispose()
    {
        EndTime = DateTime.Now;

        Latency = _sw.Elapsed;

        EmMod.Log<EmActivity>(
            $"<{ActivityId}> [{ServiceName}] " +
            $"{RequestTime:HH:mm:ss} stopped " +
            $"{Latency.TotalMilliseconds}ms " +
            $"{TokensInput}+{TokensOutput}");
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

        var begin = DateTime.Now;
        var oneHourAgo = DateTime.Now - TimeSpan.FromHours(1);

        foreach (var a in activities) {
            if (a.Status == StatusType.Completed) {
                success++;
            }

            if (begin >= a.RequestTime) {
                begin = a.RequestTime;
            }

            tokensInput += a.TokensInput;
            tokensOutput += a.TokensOutput;

            if (a.Status is not StatusType.Completed) {
                continue;
            }

            if (a.Latency > TimeSpan.Zero) {
                totalLatencySec += a.Latency.TotalSeconds;
                totalLatencyMin += a.Latency.TotalMinutes;
                latencyCount++;
            }

            if (a.EndTime > oneHourAgo) {
                tokensLastHour += a.TokensInput + a.TokensOutput;
                requestsLastHour++;
            }
        }

        return new() {
            ServiceName = serviceName,
            RequestTotal = total,
            RequestSuccess = success,
            RequestInitialTime = begin,
            TokensInput = tokensInput,
            TokensOutput = tokensOutput,
            LatencyTotalMin = totalLatencyMin,
            LatencyTotalSec = totalLatencySec,
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
        public DateTime RequestInitialTime { get; init; }
        public int RequestFailure => RequestTotal - RequestSuccess;
        public int RequestPerMin => RequestDuration.Minutes > 0 ? RequestTotal / RequestDuration.Minutes : 0;
        public int RequestSuccessPerMin => RequestDuration.Minutes > 0 ? RequestSuccess / RequestDuration.Minutes : 0;
        public TimeSpan RequestDuration => DateTime.Now - RequestInitialTime;

        public long TokensInput { get; init; }
        public long TokensOutput { get; init; }
        public int TokensLastHour { get; init; }
        public long TokensTotal => TokensInput + TokensOutput;
        public double TokensPerMin => LatencyTotalMin > 0 ? TokensTotal / LatencyTotalMin : 0;
        public double TokensPerRequest => RequestSuccess > 0 ? (double)TokensTotal / RequestSuccess : 0;

        public double LatencyTotalSec { get; init; }
        public double LatencyTotalMin { get; init; }
        public int LatencyCount { get; init; }
        public double LatencyAverage => LatencyCount > 0 ? LatencyTotalSec / LatencyCount : 0;
    }
}