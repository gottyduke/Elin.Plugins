using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cwl.Helper.String;
using Emmersive.Helper;
using HarmonyLib;

namespace Emmersive.Contexts;

public class RelationContext(IReadOnlyList<Chara> charas) : FileContextBase<RelationContext.RelationPrompt>
{
    public const char KeySeparator = '+';

    public override string Name => "relationship";

    [field: AllowNull]
    public static RelationContext Default => field ??= new(null!);

    protected override IDictionary<string, object>? BuildCore()
    {
        if (Lookup is null) {
            Init();
        }

        if (charas.Count < 2) {
            return null;
        }

        var relationKeys = GetAvailableRelationKeys(charas).ToArray();
        if (relationKeys.Length == 0) {
            return null;
        }

        Dictionary<string, object> data = [];
        foreach (var relationKey in relationKeys) {
            var relation = GetContext(relationKey);
            if (relation is null) {
                continue;
            }

            var name = charas
                .Where(c => relation.Rows.Contains(c.source))
                .Join(c => c.NameSimple);

            if (name is not null) {
                data[name] = relation.Prompt;
            }
        }

        return data.Count == 0
            ? null
            : data;
    }

    protected override RelationPrompt? LoadFromFile(FileInfo file)
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

    /// <summary>
    ///     Checks if the given characters form one or more relations defined in the lookup.
    /// </summary>
    public static bool HasAnyRelation(IEnumerable<Chara> charas)
    {
        return BuildAllRelationKeys(charas.Select(c => c.id))
            .Any(Lookup!.Contains);
    }

    /// <summary>
    ///     Gets all unique relation keys present in the lut
    ///     and can be formed from the current set of characters.
    /// </summary>
    public static IEnumerable<string> GetAvailableRelationKeys(IEnumerable<Chara> charas)
    {
        return BuildAllRelationKeys(charas.Select(c => c.id))
            .Where(k => Lookup!.Contains(k) || Overrides.ContainsKey(k))
            .Distinct();
    }

    /// <summary>
    ///     Gets a deterministic character relation key for given characters
    /// </summary>
    public static string GetRelationKey(IEnumerable<Chara> charas)
    {
        // a+b
        // a+b+c
        return string.Join(KeySeparator, charas
            .Select(c => c.id.Trim())
            .OrderBy(c => c));
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

    public static void Clear()
    {
        Lookup = null!;
        Overrides.Clear();
    }

    public static void Init()
    {
        Lookup = Default.LoadAllContexts("Emmersive/Relations").ToLookup(ctx => ctx.Key);
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

    public record RelationPrompt(string Key, IEnumerable<SourceChara.Row> Rows, string Prompt, FileInfo Provider);
}