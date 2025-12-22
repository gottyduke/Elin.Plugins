using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cwl.Helper.String;

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
    private static readonly HashSet<string> _services = new(StringComparer.Ordinal);

    private readonly long _start;

    private EmActivity()
    {
        _start = Stopwatch.GetTimestamp();
        ActivityId = InternalCount;
    }

    private static int InternalCount => ++field;

    public static EmActivity? Current { get; private set; }

    public DateTime RequestTime { get; private init; }
    public DateTime EndTime => RequestTime + Latency;
    public int TokensInput { get; set; }
    public int TokensOutput { get; set; }
    public TimeSpan Latency { get; set; } = TimeSpan.Zero;
    public StatusType Status { get; private set; } = StatusType.Unknown;

    public required string ServiceName { get; init; }
    public int ActivityId { get; }

    public void Dispose()
    {
        Latency = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - _start) / (double)Stopwatch.Frequency);

        EmMod.Log<EmActivity>(
            $"<{ActivityId}> [{ServiceName}] " +
            $"{EndTime.ToLocalTime().ToLongTimeString()} stopped " +
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
            RequestTime = DateTime.UtcNow,
        };

        Session.Add(activity);

        EmMod.Log<EmActivity>(
            $"<{activity.ActivityId}> [{activity.ServiceName}] " +
            $"{activity.RequestTime.ToLocalTime().ToLongTimeString()} started");

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

    public static EmActivitySummary GetSummary(string serviceName = "")
    {
        var activities = serviceName.IsEmptyOrNull ? Session : FromProvider(serviceName).ToList();
        var total = activities.Count;

        var success = 0;
        var tokensInput = 0;
        var tokensOutput = 0;
        var totalLatencySec = 0d;
        var totalLatencyMin = 0d;
        var latencyCount = 0;
        var tokensLastHour = 0;
        var requestsLastHour = 0;

        var begin = DateTime.UtcNow;
        var oneHourAgo = DateTime.UtcNow - TimeSpan.FromHours(1);

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

        public int RequestPerMin =>
            (int)Math.Round(RequestDuration.TotalMinutes > 0 ? RequestTotal / RequestDuration.TotalMinutes : 0);

        public int RequestSuccessPerMin =>
            (int)Math.Round(RequestDuration.TotalMinutes > 0 ? RequestSuccess / RequestDuration.TotalMinutes : 0);

        public TimeSpan RequestDuration => DateTime.UtcNow - RequestInitialTime;

        public long TokensInput { get; init; }
        public long TokensOutput { get; init; }
        public int TokensLastHour { get; init; }
        public long TokensTotal => TokensInput + TokensOutput;
        public double TokensPerMin => RequestDuration.Minutes > 0 ? TokensTotal / (double)RequestDuration.Minutes : 0;
        public double TokensPerRequest => RequestSuccess > 0 ? (double)TokensTotal / RequestSuccess : 0;

        public double LatencyTotalSec { get; init; }
        public double LatencyTotalMin { get; init; }
        public int LatencyCount { get; init; }
        public double LatencyAverage => LatencyCount > 0 ? LatencyTotalSec / LatencyCount : 0;
    }
}