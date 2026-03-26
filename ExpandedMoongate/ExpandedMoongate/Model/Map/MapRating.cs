using Newtonsoft.Json;

namespace Exm.Model.Map;

public sealed record MapRating
{
    /// <summary>
    ///     Map id
    /// </summary>
    [JsonProperty("map_id")]
    public required string MapId { get; init; }

    /// <summary>
    ///     Rating author
    /// </summary>
    [JsonProperty("user_id")]
    public required string UserId { get; init; }

    /// <summary>
    ///     Rating date
    /// </summary>
    [JsonProperty("rated_at")]
    public string? RatedAt { get; init; }

    /// <summary>
    ///     Visit date
    /// </summary>
    [JsonProperty("visited_at")]
    public string? VisitedAt { get; init; }
}