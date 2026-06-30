using System;
using System.Collections.Generic;
using System.Linq;

namespace Emmersive.Helper;

public static class LinqExt
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        public IEnumerable<TSource> OfDerived(Type baseType)
        {
            return source.Where(t => baseType.IsAssignableFrom(t as Type));
        }

        public IEnumerable<TSource> Flatten()
        {
            foreach (var item in source) {
                if (item is IEnumerable<TSource> subList && item is not string) {
                    foreach (var subItem in subList.Flatten()) {
                        yield return subItem;
                    }
                } else {
                    yield return item;
                }
            }
        }
    }
}