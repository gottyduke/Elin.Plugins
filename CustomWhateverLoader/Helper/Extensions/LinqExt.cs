using System;
using System.Collections.Generic;

namespace Cwl.Helper.Extensions;

public static class LinqExt
{
    public static IEnumerable<TSource> MissingFrom<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second,
        Action<TSource>? onPresent = null)
    {
        Dictionary<TSource, int> occurred = [];
        var nullCount = 0;

        foreach (var item in second) {
            if (item == null) {
                nullCount++;
            } else {
                occurred.TryAdd(item, 0);
                occurred[item]++;
            }
        }

        foreach (var item in first) {
            if (item == null) {
                nullCount--;
                if (nullCount < 0) {
                    yield return item;
                }
            } else {
                if (occurred.TryGetValue(item, out var count)) {
                    if (count == 0) {
                        occurred.Remove(item);
                        yield return item;
                    } else {
                        occurred[item]--;
                        onPresent?.Invoke(item);
                    }
                } else {
                    yield return item;
                }
            }
        }
    }
}