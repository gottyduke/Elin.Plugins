using System;
using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper;

public static class ReverseId
{
    private static readonly Dictionary<string, int> _cached = [];
    private static Dictionary<string, BGMData> _lookup = [];

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

    public static int BGM(string bgmName, int fallback = -1)
    {
        var id = fallback;

        if (_lookup.Count != Core.Instance.refs.bgms.Count) {
            _lookup = Core.Instance.refs.bgms.ToDictionary(kv => kv.name, kv => kv);
        }

        if (_lookup.TryGetValue(bgmName, out var data) || _lookup.TryGetValue($"BGM/{bgmName}", out data)) {
            id = data.id;
        }

        return id;
    }

    public static string HashKey(this Card card)
    {
        return $"{card.id}/{card.uid}";
    }
}