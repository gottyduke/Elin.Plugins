using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.API.Services;
using Emmersive.Helper;
using Newtonsoft.Json;

namespace Emmersive.Contexts.Memory;

internal sealed class MemoryLexicon
{
    private const string ResourceKey = "Emmersive/MemoryLexicon.json";

    private static readonly JsonSerializerSettings _mergeSettings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    private static MemoryLexicon? _current;
    private static string? _source;

    internal static MemoryLexicon Current
    {
        get {
            if (_current is null || _source is not null) {
                var source = ResourceFetch.GetActiveResource(ResourceKey);
                if (_current is null || !ReferenceEquals(source, _source)) {
                    _current = Load(source);
                    _source = source;
                }
            }

            return _current;
        }
        set {
            _current = value;
            _source = null;
        }
    }

    [JsonProperty("player_words")]
    public List<string> PlayerWords { get; set; } = [];

    [JsonProperty("stop_words")]
    public HashSet<string> StopWords { get; set; } = [];

    [JsonProperty("stop_han")]
    public string StopHan { get; set; } = "";

    [JsonProperty("stop_bigrams")]
    public HashSet<string> StopBigrams { get; set; } = [];

    [JsonProperty("negation_words")]
    public HashSet<string> NegationWords { get; set; } = [];

    [JsonProperty("significant_chars")]
    public string SignificantChars { get; set; } = "";

    [JsonProperty("hours_ago")]
    public string HoursAgo { get; set; } = "";

    [JsonProperty("days_ago")]
    public string DaysAgo { get; set; } = "";

    [JsonIgnore]
    internal HashSet<string> PlayerTokens => field ??= [..PlayerWords.SelectMany(MemoryRelevance.Tokenize)];

    internal static MemoryLexicon Merge(params string?[] sources)
    {
        var lexicon = new MemoryLexicon();
        foreach (var json in sources) {
            if (!json.IsWhiteSpaceOrNull) {
                JsonConvert.PopulateObject(json!, lexicon, _mergeSettings);
            }
        }

        return lexicon;
    }

    private static MemoryLexicon Load(string source)
    {
        var defaults = ResourceFetch.GetDefaultResource(ResourceKey);
        try {
            return Merge(defaults, source);
        } catch (Exception ex) {
            EmMod.Warn<MemoryLexicon>($"{ResourceKey} is not valid json, using the built-in one\n{ex.Message}");
            return Merge(defaults);
        }
    }
}