namespace Cwl.API;

public sealed record SerializableAchievement : SerializableAchievementV1;

public record SerializableAchievementV1
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Prerequisite { get; init; }
    public float? AutoUnlockProgress { get; init; }
}