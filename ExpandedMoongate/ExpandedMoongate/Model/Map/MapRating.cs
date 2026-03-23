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
    [JsonProperty("author")]
    public required string Author { get; init; }

    /// <summary>
    ///     Rating score, 1 - 5
    /// </summary>
    [JsonProperty("score")]
    public required int Score { get; init; }

    /// <summary>
    ///     Rating date
    /// </summary>
    [JsonProperty("rated_at")]
    public string? RatedAt { get; init; }

    /// <summary>
    ///     Rating comment
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; init; }

    // d1
    [JsonProperty("uuid")]
    public string? RatingUuid { get; init; }
}