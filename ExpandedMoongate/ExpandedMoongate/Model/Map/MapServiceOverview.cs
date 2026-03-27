using Newtonsoft.Json;

namespace Exm.Model.Map;

public sealed record MapServiceOverview
{
    /// <summary>
    ///     Total maps hosted
    /// </summary>
    [JsonProperty("maps_count")]
    public required int MapsCount { get; init; }

    /// <summary>
    ///     Total ratings tracked
    /// </summary>
    [JsonProperty("ratings_count")]
    public required int RatingsCount { get; init; }

    /// <summary>
    ///     Total visits occurred
    /// </summary>
    [JsonProperty("visits_count")]
    public required int VisitsCount { get; init; }

    /// <summary>
    ///     Maps uploaded last 24 hours
    /// </summary>
    [JsonProperty("maps_today")]
    public required int MapsToday { get; init; }

    /// <summary>
    ///     Ratings tracked last 24 hours
    /// </summary>
    [JsonProperty("ratings_today")]
    public required int RatingsToday { get; init; }

    /// <summary>
    ///     Visits occurred last 24 hours
    /// </summary>
    [JsonProperty("visits_today")]
    public required int VisitsToday { get; init; }
}