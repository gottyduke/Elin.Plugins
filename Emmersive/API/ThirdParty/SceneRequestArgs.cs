namespace Emmersive.API.ThirdParty;

public sealed class SceneRequestArgs
{
    public required bool Success { get; init; }

    public string? ProviderId { get; init; }

    public string? Error { get; init; }

    public string? RawContent { get; init; }
}