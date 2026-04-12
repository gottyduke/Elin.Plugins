using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper;

public static class ReverseId
{
    private static Dictionary<string, BGMData> _lookup = [];

    public static int Material(string materialAlias, int fallback = -1)
    {
        var id = fallback;
        if (EMono.sources.materials.alias.TryGetValue(materialAlias, out var row)) {
            id = EMono.sources.materials.rows.IndexOf(row);
        }

        return id;
    }

    public static int Block(string blockAlias, int fallback = -1)
    {
        var id = fallback;
        if (EMono.sources.blocks.alias.TryGetValue(blockAlias, out var row)) {
            id = EMono.sources.blocks.rows.IndexOf(row);
        }

        return id;
    }

    public static int Obj(string objAlias, int fallback = -1)
    {
        var id = fallback;
        if (EMono.sources.objs.alias.TryGetValue(objAlias, out var row)) {
            id = EMono.sources.objs.rows.IndexOf(row);
        }

        return id;
    }

    public static int BGM(string bgmName, int fallback = -1)
    {
        var id = fallback;

        if (_lookup.Count != Core.Instance.refs.bgms.Count) {
            _lookup = Core.Instance.refs.bgms.ToDictionary(kv => kv.name);
        }

        if (_lookup.TryGetValue(bgmName, out var data) || _lookup.TryGetValue($"BGM/{bgmName}", out data)) {
            id = data.id;
        }

        return id;
    }
}