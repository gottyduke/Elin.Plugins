using Newtonsoft.Json;

namespace Exm.Model.Map;

public sealed record MapMeta
{
    // 0
    // on elin server stored as files/XX/id.z
    [JsonProperty("id")]
    public required string Id { get; init; }

    // 1 normal id
    // 2
    [JsonProperty("author")]
    public required string Author { get; init; }

    // 3
    [JsonProperty("title")]
    public required string Title { get; init; }

    // 4
    [JsonProperty("language")]
    public string? Lang { get; init; }

    // 5
    [JsonProperty("category")]
    public string? Category { get; init; }

    // 6
    [JsonProperty("created_at")]
    public required string Date { get; init; }

    // 7 - IP - redacted
    // 8
    [JsonProperty("version")]
    public required int Version { get; init; }

    // 9
    [JsonProperty("tag")]
    public string? Tag { get; init; }

    // stats
    [JsonProperty("visit_count")]
    public int VisitCount { get; init; }

    [JsonProperty("rating_count")]
    public int RatingCount { get; init; }

    [JsonProperty("rating_average")]
    public float RatingAverage { get; init; }

    // d1 -> r2
    [JsonProperty("file_key")]
    public string? FileKey { get; init; }

    [JsonProperty("file_size")]
    public int FileSize { get; init; }

    [JsonProperty("preview_key")]
    public string? PreviewKey { get; init; }

    [JsonIgnore]
    public bool IsValidVersion => BaseCore.Instance.version.GetInt() >= Version;
}