using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSS.Helper;

public static class ReversePartId
{
    private static List<PCC.Part> _cache = [];

    public static void BuildPartCache(PCC pcc)
    {
        _cache = pcc.GetBodySet().map.Values
            .SelectMany(ps => ps.map.Values)
            .ToList();
    }

    public static string GetPartId(this Texture2D texture)
    {
        var part = _cache.FirstOrDefault(p => p.modTextures.Values.Any(mi => mi.cache == texture));
        return part?.idFull ?? string.Empty;
    }
}