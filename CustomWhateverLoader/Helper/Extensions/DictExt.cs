using System;
using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper.Extensions;

public static class DictExt
{
    public static int GetContentHashCode<TValue>(this Dictionary<string, TValue> dict)
    {
        return dict.Keys.Aggregate(dict.Count,
            (current, key) => unchecked((current << 5) - current + StringComparer.InvariantCultureIgnoreCase.GetHashCode(key)));
    }

    extension<TValue>(Dictionary<int, TValue> dict)
    {
        public int NextUniqueKey(int begin = -1, int step = -1)
        {
            var key = begin;
            while (dict.ContainsKey(key)) {
                key += step;
            }

            return key;
        }

        public void Set(string key, TValue value)
        {
            dict[key.GetHashCode()] = value;
        }

        public bool ContainsKey(string key)
        {
            return dict.ContainsKey(key.GetHashCode());
        }

        public bool TryGetValue(string key, out TValue value)
        {
            return dict.TryGetValue(key.GetHashCode(), out value);
        }
    }
}