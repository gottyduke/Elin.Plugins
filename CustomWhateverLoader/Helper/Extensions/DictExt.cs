using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.String;

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

        public void Remove(string key)
        {
            dict.Remove(key.GetHashCode());
        }

        public bool TryGetValue(string key, out TValue value)
        {
            return dict.TryGetValue(key.GetHashCode(), out value);
        }
    }

    public class FileInfoComparer : IEqualityComparer<FileInfo>
    {
        public static FileInfoComparer Default => field ??= new();

        public bool Equals(FileInfo? lhs, FileInfo? rhs)
        {
            if (ReferenceEquals(lhs, rhs)) {
                return true;
            }

            if (lhs is null || rhs is null) {
                return false;
            }

            return string.Equals(lhs.FullName.NormalizePath(), rhs.FullName.NormalizePath(),
                StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(FileInfo file)
        {
            return file.FullName.NormalizePath().GetHashCode(StringComparison.InvariantCultureIgnoreCase);
        }
    }
}