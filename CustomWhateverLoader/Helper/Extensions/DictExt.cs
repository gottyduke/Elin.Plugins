using System.Collections.Generic;

namespace Cwl.Helper.Extensions;

public static class DictExt
{
    public static int NextUniqueKey<T>(this Dictionary<int, T> dict, int begin = -1, int step = -1)
    {
        var key = begin;
        while (dict.ContainsKey(key)) {
            key += step;
        }

        return key;
    }

    public static void TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> kv)
    {
        dict.TryAdd(kv.Key, kv.Value);
    }
}