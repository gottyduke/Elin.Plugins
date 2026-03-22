using System;
using System.Collections.Generic;

internal static class CollectionHelper
{
    extension(ThingContainer things)
    {
        internal IEnumerable<Thing> Flatten()
        {
            foreach (var t1 in things) {
                if (t1.things.Count == 0) {
                    yield return t1;
                    continue;
                }

                foreach (var t2 in t1.things.Flatten()) {
                    yield return t2;
                }
            }
        }
    }

    extension<T>(IEnumerable<T> collection)
    {
        internal void ForEach(Action<T> action)
        {
            foreach (var item in collection) {
                action(item);
            }
        }
    }
}