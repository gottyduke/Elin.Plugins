using System;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

public sealed class MemoryEntry
{
    [JsonProperty("t")]
    public int Turn { get; init; }

    [JsonProperty("ts")]
    public DateTime Time { get; init; } = DateTime.UtcNow;

    [JsonProperty("s")]
    public required string Speaker { get; init; }

    [JsonProperty("c")]
    public required string Content { get; init; }

    [JsonProperty("i")]
    [JsonConverter(typeof(RangedIntConverter), 1, 5)]
    public int Importance { get; set; } = 1;

    public override string ToString()
    {
        return $"[{Speaker}]: {Content}";
    }
}