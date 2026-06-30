using System;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

public class MemoryFact
{
    [JsonProperty("c")]
    public DateTime Created { get; init; } = DateTime.UtcNow;

    [JsonProperty("lr")]
    public DateTime? LastRecalled { get; set; }

    [JsonProperty("f")]
    public required string Fact { get; set; }

    [JsonProperty("i")]
    [JsonConverter(typeof(RangedIntConverter), 1, 5)]
    public int Importance { get; set; } = 1;

    [JsonProperty("rc")]
    public int RecallCount { get; set; }

    public void MarkRecalled()
    {
        LastRecalled = DateTime.UtcNow;
        RecallCount++;
    }

    public override string ToString()
    {
        return $"[★{Importance}] {Fact}";
    }
}