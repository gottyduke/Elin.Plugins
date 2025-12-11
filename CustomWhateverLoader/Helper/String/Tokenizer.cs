using System;
using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper.String;

public static class Tokenizer
{
    private static readonly Dictionary<object, Dictionary<string, string>> _cached = [];

    public static int ComputeLevenshteinDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.Length == 0) {
            return b.Length;
        }

        if (b.Length == 0) {
            return a.Length;
        }

        Span<int> previous = stackalloc int[b.Length + 1];
        Span<int> current = stackalloc int[b.Length + 1];

        for (var i = 0; i <= b.Length; ++i) {
            previous[i] = i;
        }

        for (var i = 1; i <= a.Length; ++i) {
            current[0] = i;
            for (var j = 1; j <= b.Length; ++j) {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            // swaps
            var temp = previous;
            previous = current;
            current = temp;
        }

        return previous[b.Length];
    }

    extension(object? args)
    {
        public Dictionary<string, string> Tokenize()
        {
            if (args is null) {
                return [];
            }

            if (_cached.TryGetValue(args, out var tokens)) {
                return tokens;
            }

            Dictionary<string, object?> store = [];
            if (args is IDictionary<string, object?> input) {
                foreach (var (k, v) in input) {
                    store[k] = v;
                }
            } else {
                foreach (var property in args.GetType().GetProperties()) {
                    store[property.Name] = property.GetValue(args);
                }

                foreach (var field in args.GetType().GetCachedFields()) {
                    store[field.Name] = field.GetValue(args);
                }
            }

            foreach (var (k, v) in store.ToArray()) {
                if (v is null) {
                    store.Remove(k);
                }
            }

            return _cached[args] = store.ToDictionary(k => k.Key, v => v.Value!.ToString());
        }
    }
}