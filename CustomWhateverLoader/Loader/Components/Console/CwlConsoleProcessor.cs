using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.String;
using ReflexCLI.Parameters;

namespace Cwl.Components;

[ParameterProcessor(typeof(Chara))]
internal class CharaParameterProcessor : ParameterProcessor
{
    public override object ConvertString(Type type, string inString)
    {
        return EClass._map.FindChara(inString);
    }

    public override List<Suggestion> GetSuggestions(Type type, string subStr, object[] attributes, int maxResults)
    {
        if (subStr.IsEmptyOrNull) {
            return [];
        }

        var searchSpan = subStr.Trim().AsSpan();
        List<Suggestion> results = [];

        var charas = EClass._map.charas;

        foreach (var chara in charas) {
            if (chara.id.StartsWith(searchSpan, StringComparison.OrdinalIgnoreCase)) {
                results.Add(new(chara.id, chara.Name));
            }
        }

        if (results.Count < maxResults) {
            foreach (var chara in charas) {
                if (results.Find(s => string.Equals(chara.id, s.Value, StringComparison.OrdinalIgnoreCase)) is not null) {
                    continue;
                }

                if (chara.id.IndexOf(searchSpan, StringComparison.OrdinalIgnoreCase) >= 0) {
                    results.Add(new(chara.id, chara.Name));
                }
            }
        }

        if (results.Count >= maxResults) {
            return results.Take(maxResults).ToList();
        }

        // fuzzy
        foreach (var chara in charas) {
            if (results.Count >= maxResults) {
                break;
            }

            if (results.Exists(s => s.Value.Equals(chara.id, StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            if (Tokenizer.ComputeLevenshteinDistance(chara.id, searchSpan) <= 2) {
                results.Add(new(chara.id, chara.Name));
            }
        }

        return results
            .Take(maxResults)
            .ToList();
    }
}