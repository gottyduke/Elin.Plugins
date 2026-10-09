namespace Emmersive.API.ThirdParty;

public sealed class SceneLineArgs
{
    public required Chara Chara { get; init; }

    public required string Text { get; set; }

    public required bool IsGesture { get; init; }

    public required bool IsPlayer { get; init; }

    public float Duration { get; set; }

    public bool Suppress { get; set; }
}