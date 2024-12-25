using System;
using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper;

public class ReverseId : EClass
{
    private static readonly Dictionary<string, int> _cached = [];

    public static int Material(string materialName)
    {
        if (_cached.TryGetValue(materialName, out var id)) {
            return id;
        }

        id = sources.materials.map
            .Where(kv => string.Equals(kv.Value.alias, materialName.Trim(), StringComparison.CurrentCultureIgnoreCase))
            .Select(kv => (int?)kv.Key)
            .FirstOrDefault() ?? -1;

        _cached[materialName] = id;
        return id;
    }
}