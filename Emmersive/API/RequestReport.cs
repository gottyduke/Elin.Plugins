namespace Emmersive.API;

public enum RequestStatus
{
    Success,
    NoProvider,
    Unavailable,
    Timeout,
    Cancelled,
    HttpError,
    EmptyResponse,
    DryRun,
    Failed,
}

public sealed class RequestReport
{
    public bool Success { get; init; }

    public RequestStatus Status { get; init; }

    public string? Content { get; init; }

    public string? ErrorReason { get; init; }

    public string? ProviderId { get; init; }

    public double LatencyMs { get; init; }

    public int TokensInput { get; init; }

    public int TokensOutput { get; init; }

    public EmActivity.EmActivitySummary? ProviderSummary { get; init; }

    internal string? LocalizedError { get; init; }

    internal static RequestReport Ok(string content, string providerId, EmActivity activity)
    {
        return new() {
            Success = true,
            Status = RequestStatus.Success,
            Content = content,
            ProviderId = providerId,
            LatencyMs = activity.Elapsed.TotalMilliseconds,
            TokensInput = activity.TokensInput,
            TokensOutput = activity.TokensOutput,
            ProviderSummary = EmActivity.GetSummary(providerId),
        };
    }

    internal static RequestReport Fail(string reason, string? providerId = null)
    {
        return Fail(RequestStatus.Failed, reason, providerId);
    }

    internal static RequestReport Fail(RequestStatus status,
                                       string reason,
                                       string? providerId = null,
                                       string? localizedError = null)
    {
        return new() {
            Success = false,
            Status = status,
            ErrorReason = reason,
            ProviderId = providerId,
            ProviderSummary = providerId is not null ? EmActivity.GetSummary(providerId) : null,
            LocalizedError = localizedError,
        };
    }
}