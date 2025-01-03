using System;
using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper;

public static class ReverseId
{
    private static readonly Dictionary<string, int> _cached = [];

    public static int Material(string materialName, int fallback = -1)
    {
        var cache = $"{nameof(SourceMaterial)}/{materialName}";
        if (_cached.TryGetValue(cache, out var id)) {
            return id;
        }

        var row = EClass.sources.materials.map
            .Where(kv =>
                string.Equals(kv.Value.alias, materialName.Trim(), StringComparison.InvariantCultureIgnoreCase))
            .Select(kv => kv.Value)
            .FirstOrDefault();
        id = EClass.sources.materials.rows.IndexOf(row);
        if (id is -1) {
            id = fallback;
        }

        _cached[cache] = id;
        return id;
    }

    public static int NextUniqueKey<T>(this Dictionary<int, T> dict, int step = -1)
    {
        var key = -1;
        while (dict.ContainsKey(key)) {
            key += step;
        }

        return key;
    }
}