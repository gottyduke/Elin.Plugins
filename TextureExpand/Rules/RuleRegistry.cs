using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextureExpand.Textures;

namespace TextureExpand.Rules;

internal static class RuleRegistry
{
    internal static readonly Dictionary<int, List<ReplaceRule>> ByTile = [];
    internal static readonly Dictionary<string, List<ReplaceRule>> ByCharaId = [];
    internal static readonly Dictionary<int, List<SourceChara.Row>> RowsByTile = [];

    internal static bool HasRules { get; private set; }

    internal static int Count => ByTile.Values.Sum(x => x.Count) + ByCharaId.Values.Sum(x => x.Count);

    internal static DateTime Build(List<ReplaceGroup> groups)
    {
        var latest = DateTime.MinValue;
        var layersByTile = new Dictionary<int, Dictionary<string, List<ReplaceRule>>>();
        var layersByChara = new Dictionary<string, Dictionary<string, List<ReplaceRule>>>();
        var charas = EClass.sources.charas.map;
        IndexRows(charas.Values);

        foreach (var group in groups) {
            if (group.TileId >= 0) {
                if (ByTile.ContainsKey(group.TileId)) {
                    continue;
                }

                var rules = ByTile[group.TileId] = [];
                if (!RowsByTile.TryGetValue(group.TileId, out var rows)) {
                    continue;
                }

                foreach (var row in rows) {
                    if (!RenderDataExpander.HasSourceTexture(row)) {
                        TextureExpand.LogError("Not found RenderData: " + row._idRenderData);
                        continue;
                    }

                    AddDefaultRules(row, group.TileId, rules);
                    AddRules(row.id, group, rules, layersByTile, group.TileId, ref latest);
                }
            } else {
                if (group.CharaId.IsEmpty() || !charas.TryGetValue(group.CharaId!, out var row)) {
                    continue;
                }

                if (!RenderDataExpander.HasSourceTexture(row)) {
                    TextureExpand.LogError("Not found RenderData: " + row._idRenderData);
                    continue;
                }

                if (!ByCharaId.TryGetValue(row.id, out var rules)) {
                    rules = ByCharaId[row.id] = [];
                    AddDefaultRules(row, -1, rules);
                }

                AddRules(row.id, group, rules, layersByChara, row.id, ref latest);
            }
        }

        MergeLayers(layersByTile, ByTile, true);
        MergeLayers(layersByChara, ByCharaId, false);

        foreach (var rules in ByTile.Values.Concat(ByCharaId.Values)) {
            rules.Sort((a, b) => b.Priority - a.Priority);
        }

        HasRules = Count > 0;
        return latest;
    }

    private static void IndexRows(IEnumerable<SourceChara.Row> rows)
    {
        RowsByTile.Clear();
        foreach (var row in rows) {
            foreach (var tile in row.tiles.Concat(row.tiles_snow).Distinct()) {
                if (!RowsByTile.TryGetValue(tile, out var list)) {
                    RowsByTile[tile] = list = [];
                }

                list.Add(row);
            }
        }
    }

    private static void AddDefaultRules(SourceChara.Row row, int targetTile, List<ReplaceRule> rules)
    {
        foreach (var tile in row.tiles.Concat(row.tiles_snow)) {
            if (targetTile >= 0 && tile != targetTile) {
                continue;
            }

            var key = $"objC_{tile}";
            if (rules.Any(x => x.Key == key)) {
                continue;
            }

            var rule = ReplaceRule.Create(row.id, key, tile.ToString());
            if (rule is not null) {
                rules.Add(rule);
            }
        }
    }

    private static void AddRules<TKey>(string charaId,
                                       ReplaceGroup group,
                                       List<ReplaceRule> rules,
                                       Dictionary<TKey, Dictionary<string, List<ReplaceRule>>> layers,
                                       TKey layerKey,
                                       ref DateTime latest)
    {
        foreach (var (key, path) in group.Entries) {
            var rule = ReplaceRule.Create(charaId, key, path);
            if (rule is null) {
                continue;
            }

            var written = File.GetLastWriteTime(path);
            if (written > latest) {
                latest = written;
            }

            if (rule.LayerIndex is not null) {
                if (!layers.TryGetValue(layerKey, out var byPhrase)) {
                    layers[layerKey] = byPhrase = [];
                }

                if (!byPhrase.TryGetValue(rule.ConcatMatchPhrase, out var layerRules)) {
                    byPhrase[rule.ConcatMatchPhrase] = layerRules = [];
                }

                layerRules.Add(rule);
                continue;
            }

            var existing = rules.Find(x => x.ConcatMatchPhrase == rule.ConcatMatchPhrase);
            if (existing is not null) {
                if (rule.Priority < existing.Priority) {
                    continue;
                }

                rules.Remove(existing);
            }

            rules.Add(rule);
        }
    }

    private static void MergeLayers<TKey>(Dictionary<TKey, Dictionary<string, List<ReplaceRule>>> layers,
                                          Dictionary<TKey, List<ReplaceRule>> rulesByKey,
                                          bool dedupe)
    {
        foreach (var kv in layers) {
            if (!rulesByKey.TryGetValue(kv.Key, out var rules)) {
                continue;
            }

            foreach (var layerRules in kv.Value.Values) {
                for (var i = rules.Count - 1; i >= 0; --i) {
                    var merged = ReplaceRule.AddLayer(rules[i], layerRules);
                    if (merged is null) {
                        continue;
                    }

                    if (!dedupe) {
                        rules.Add(merged);
                        continue;
                    }

                    if (rules.Any(x => x.HasJudges && x.ConcatFilePath == merged.ConcatFilePath && merged.ContainsJudgeAll(x))) {
                        continue;
                    }

                    var superset = rules.FindIndex(x =>
                        x.HasJudges && x.ConcatFilePath == merged.ConcatFilePath && x.ContainsJudgeAll(merged));
                    if (superset >= 0) {
                        rules[superset] = merged;
                    }

                    rules.Add(merged);
                }
            }
        }
    }
}