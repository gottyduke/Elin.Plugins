using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Emmersive.Helper;
using HarmonyLib;

namespace Emmersive.Contexts;

public class RelationContext(IReadOnlyList<Chara> charas) : ContextProviderBase
{
    public const char KeySeparator = '+';

    [field: AllowNull]
    public static ILookup<string, RelationPrompt> Lookup
    {
        get => field ??= BuildLookup();
        private set;
    }

    public override string Name => "relationship";

    protected override void Localize(IDictionary<string, object> data, string? prefixOverride = null)
    {
        // no need to localize character names
    }

    protected override IDictionary<string, object>? BuildInternal()
    {
        if (charas.Count < 2) {
            return null;
        }

        var charaIds = charas.Select(c => c.UnifiedId);
        var relationKeys = BuildAllRelationKeys(charaIds).ToArray();
        if (relationKeys.Length == 0) {
            return null;
        }

        Dictionary<string, object> data = [];
        foreach (var relationKey in relationKeys) {
            var resourceKey = $"Emmersive/Relations/{relationKey}.txt";

            var active = ResourceFetch.GetActiveResource(resourceKey, false);
            if (active.IsEmpty()) {
                var relation = Lookup[relationKey].LastOrDefault();
                if (relation is null || relation.Prompt.IsEmpty()) {
                    continue;
                }

                active = relation.Prompt;

                ResourceFetch.SetActiveResource(resourceKey, active);
            }

            var name = SplitByRelationKey(relationKey, charas)
                .Join(c => c.NameSimple);

            if (!name.IsEmpty() && !active.IsEmpty()) {
                data[name] = active;
            }
        }

        return data.Count == 0
            ? null
            : data;
    }

#region RelationKey

    public static IEnumerable<Chara> SplitByRelationKey(string relationKey, IEnumerable<Chara> charas)
    {
        var charaIds = relationKey.Split(KeySeparator);
        return charas
            .Where(c => charaIds.Contains(c.id));
    }

    /// <summary>
    ///     Gets a deterministic character relation key for given character IDs
    /// </summary>
    public static string GetRelationKey(IEnumerable<string> charaIds)
    {
        // a+b
        // a+b+c
        return string.Join(KeySeparator, charaIds
            .Select(id => id.Trim())
            .OrderBy(id => id));
    }

    // O(2^N)
    private static IEnumerable<string> BuildAllRelationKeys(IEnumerable<string> charaIds)
    {
        var charaList = charaIds.ToList();
        var count = charaList.Count;
        HashSet<string> keys = [];

        for (var i = 2; i <= count; ++i) {
            var subsets = 1 << count;
            for (var j = 0; j < subsets; ++j) {
                if (BitOperations.PopCount(j) != i) {
                    continue;
                }

                List<string> relations = [];
                for (var chara = 0; chara < count; ++chara) {
                    var bit = j & (1 << chara);
                    if (bit != 0) {
                        relations.Add(charaList[chara]);
                    }
                }

                keys.Add(GetRelationKey(relations));
            }
        }

        return keys;
    }

#endregion

#region RelationPrompt

    public static ILookup<string, RelationPrompt> BuildLookup()
    {
        using var _ = PackageIterator.AddTempLookupPaths(ResourceFetch.CustomFolder);
        return PackageIterator
            .GetRelocatedDirsFromPackage("Emmersive/Relations")
            .SelectMany(d => d.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            .Select(LoadFromFile)
            .OfType<RelationPrompt>()
            .ToLookup(rp => rp.Key);
    }

    public static void Clear()
    {
        Lookup = null!;
        EmMod.Log<RelationContext>("cleared relation prompts");
    }

    private static RelationPrompt? LoadFromFile(FileInfo file)
    {
        var lines = File.ReadAllLines(file.FullName);
        if (lines.Length < 2) {
            EmMod.Warn<RelationPrompt>("relation prompt must contain at least 2 lines");
            return null;
        }

        var charaIds = lines[0].Split(KeySeparator);
        var sources = EMono.sources.charas.map;
        List<SourceChara.Row> rows = [];

        foreach (var id in charaIds) {
            if (!sources.TryGetValue(id, out var row)) {
                EmMod.Warn<RelationPrompt>($"relation prompt uses invalid chara id: {id}");
                return null;
            }

            rows.Add(row);
        }

        var relationKey = GetRelationKey(charaIds);
        var prompt = string.Join(' ', lines.Skip(1));

        var length = prompt.Length;
        if (length < 10) {
            EmMod.Warn<RelationPrompt>("relation prompt must be longer than 10 characters");
            return null;
        }

        EmMod.Debug<RelationPrompt>($"{relationKey} - {length} - {file.ShortPath()}");

        return new(relationKey, rows, prompt, file);
    }

    public record RelationPrompt(string Key, IEnumerable<SourceChara.Row> Rows, string Prompt, FileInfo Provider);

#endregion
}