using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

public class MemoryFact
{
    private string? _tokenSource;
    private HashSet<string>? _tokens;

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

    [JsonProperty("gt")]
    public int GameTime { get; set; }

    [JsonIgnore]
    internal int? LastInjectedTick { get; set; }

    [JsonIgnore]
    internal int? SaidUntilTick { get; set; }

    [JsonIgnore]
    internal HashSet<string> Tokens
    {
        get {
            if (_tokens is null || _tokenSource != Fact) {
                _tokenSource = Fact;
                _tokens = MemoryRelevance.Tokenize(Fact);
            }

            return _tokens;
        }
    }

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