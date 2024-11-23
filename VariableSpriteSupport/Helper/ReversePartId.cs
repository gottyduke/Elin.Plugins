using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSS.Helper;

internal static class ReversePartId
{
    private static List<PCC.Part> _cache = [];

    internal static void BuildPartCache(PCC pcc)
    {
        _cache = pcc.GetBodySet().map.Values
            .SelectMany(ps => ps.map.Values)
            .ToList();
    }

    internal static string GetPartId(this Texture2D texture)
    {
        var part = _cache.FirstOrDefault(p => p.modTextures.Values.Any(mi => mi.cache == texture));
        return part?.idFull ?? string.Empty;
    }
}