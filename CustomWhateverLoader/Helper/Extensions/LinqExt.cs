using System;
using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper.Extensions;

public static class LinqExt
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        public IEnumerable<TSource> OfDerived(Type baseType)
        {
            return source.Where(t => baseType.IsAssignableFrom(t as Type));
        }

        public IEnumerable<TSource> MissingFrom(IEnumerable<TSource> second,
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

            foreach (var item in source) {
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

        public IEnumerable<TSource> Flatten()
        {
            foreach (var item in source) {
                if (item is IEnumerable<TSource> subList && item is not string) {
                    foreach (var subItem in Flatten(subList)) {
                        yield return subItem;
                    }
                } else {
                    yield return item;
                }
            }
        }
    }
}