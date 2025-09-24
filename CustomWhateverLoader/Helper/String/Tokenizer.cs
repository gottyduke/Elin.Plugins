using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper.String;

public static class Tokenizer
{
    private static readonly Dictionary<object, Dictionary<string, string>> _cached = [];

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