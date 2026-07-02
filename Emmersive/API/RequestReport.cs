namespace Emmersive.API;

public sealed class RequestReport
{
    public bool Success { get; init; }

    public string? Content { get; init; }

    public string? ErrorReason { get; init; }

    public string? ProviderId { get; init; }

    public double LatencyMs { get; init; }

    public int TokensInput { get; init; }

    public int TokensOutput { get; init; }

    public EmActivity.EmActivitySummary? ProviderSummary { get; init; }

    internal static RequestReport Ok(string content, string providerId, EmActivity activity)
    {
        return new() {
            Success = true,
            Content = content,
            ProviderId = providerId,
            LatencyMs = activity.Latency.TotalMilliseconds,
            TokensInput = activity.TokensInput,
            TokensOutput = activity.TokensOutput,
            ProviderSummary = EmActivity.GetSummary(providerId),
        };
    }

    internal static RequestReport Fail(string reason, string? providerId = null)
    {
        return new() {
            Success = false,
            ErrorReason = reason,
            ProviderId = providerId,
            ProviderSummary = providerId is not null ? EmActivity.GetSummary(providerId) : null,
        };
    }
}