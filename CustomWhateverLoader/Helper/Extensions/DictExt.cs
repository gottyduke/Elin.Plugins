using System;
using System.Collections.Generic;

namespace Cwl.Helper.Extensions;

public static class DictExt
{
    public static int NextUniqueKey<TValue>(this Dictionary<int, TValue> dict, int begin = -1, int step = -1)
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

    public static int GetContentHashCode<TValue>(this Dictionary<string, TValue> dict)
    {
        var hash = dict.Count;
        foreach (var key in dict.Keys) {
            hash = unchecked((hash << 5) - hash + StringComparer.OrdinalIgnoreCase.GetHashCode(key));
        }

        return hash;
    }

    public static void Set<TValue>(this Dictionary<int, TValue> dict, string key, TValue value)
    {
        dict[key.GetHashCode()] = value;
    }

    public static bool ContainsKey<TValue>(this Dictionary<int, TValue> dict, string key)
    {
        return dict.ContainsKey(key.GetHashCode());
    }

    public static bool TryGetValue<TValue>(this Dictionary<int, TValue> dict, string key, out TValue value)
    {
        return dict.TryGetValue(key.GetHashCode(), out value);
    }
}