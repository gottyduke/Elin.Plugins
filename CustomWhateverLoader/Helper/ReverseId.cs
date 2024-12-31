using System;
using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper;

public class ReverseId : EClass
{
    private static readonly Dictionary<string, int> _cached = [];

    public static int Material(string materialName, int fallback = -1)
    {
        var cache = $"{nameof(SourceMaterial)}/{materialName}";
        if (_cached.TryGetValue(cache, out var id)) {
            return id;
        }

        var row = sources.materials.map
            .Where(kv =>
                string.Equals(kv.Value.alias, materialName.Trim(), StringComparison.InvariantCultureIgnoreCase))
            .Select(kv => kv.Value)
            .FirstOrDefault();
        id = sources.materials.rows.IndexOf(row);
        if (id is -1) {
            id = fallback;
        }

        _cached[cache] = id;
        return id;
    }
}