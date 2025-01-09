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
        if (id == -1) {
            id = fallback;
        }

        _cached[cache] = id;
        return id;
    }

    public static string HashKey(this Card card)
    {
        return $"{card.id}/{card.uid}";
    }
}