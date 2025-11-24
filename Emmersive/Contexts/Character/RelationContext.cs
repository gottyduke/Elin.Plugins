using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Emmersive.Helper;
using HarmonyLib;

namespace Emmersive.Contexts;

public class RelationContext(IList<Chara> charas) : ContextProviderBase
{
    public const char KeySeparator = '+';

    
    public static ILookup<string, RelationPrompt> Lookup
    {
        get => field ??= BuildLookup().ToLookup(rp => rp.Key);
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

        var data = new Dictionary<string, object>();
        foreach (var relationKey in relationKeys) {
            var relation = Lookup[relationKey].LastOrDefault();
            // kinda unnecessary check
            if (relation is null || relation.Prompt.IsEmpty()) {
                continue;
            }

            var resourceKey = $"Emmersive/Relations/{relationKey}.txt";

            var active = ResourceFetch.GetActiveResource(resourceKey, false);
            if (active.IsEmpty()) {
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
        var ids = relationKey.Split(KeySeparator)
            .ToHashSet(StringComparer.Ordinal);
        return charas.Where(c => ids.Contains(c.UnifiedId));
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

    public static IEnumerable<string> BuildAllRelationKeys(IEnumerable<string> charaIds)
    {
        var charas = charaIds
            .Select(id => id.Trim())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var relationKey in Lookup.Select(g => g.Key)) {
            var ids = relationKey.Split(KeySeparator);
            if (ids.All(charas.Contains)) {
                yield return relationKey;
            }
        }
    }

#endregion

#region RelationPrompt

    public static List<RelationPrompt> BuildLookup()
    {
        using var _ = PackageIterator.AddTempLookupPaths(ResourceFetch.CustomFolder);
        return PackageIterator
            .GetRelocatedDirsFromPackage("Emmersive/Relations")
            .SelectMany(d => d.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            .Select(LoadFromFile)
            .OfType<RelationPrompt>()
            .ToList();
    }

    public static void Clear()
    {
        Lookup = null!;
        EmMod.Log<RelationContext>("cleared relation prompts");
    }

    private static RelationPrompt? LoadFromFile(FileInfo file)
    {
        var charaIds = Path.ChangeExtension(file.Name, null).Split(KeySeparator);
        var sources = EMono.sources.charas.map;
        List<SourceChara.Row> rows = [];

        foreach (var id in charaIds) {
            if (!sources.TryGetValue(id, out var row)) {
                EmMod.Warn<RelationPrompt>($"invalid chara id: {id}");
                return null;
            }

            rows.Add(row);
        }

        var relationKey = GetRelationKey(charaIds);
        var prompt = File.ReadAllText(file.FullName);

        var length = prompt.Length;
        EmMod.Debug<RelationPrompt>($"{relationKey} - {length} - {file.ShortPath()}");

        return new(relationKey, rows, prompt, file);
    }

    public record RelationPrompt(string Key, IReadOnlyList<SourceChara.Row> Rows, string Prompt, FileInfo Provider);

#endregion
}