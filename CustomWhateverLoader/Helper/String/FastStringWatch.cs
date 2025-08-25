using System;
using System.Collections.Generic;

namespace Cwl.Helper.String;

public static class FastStringWatch
{
    private static readonly Dictionary<FastString, StringWatch> _cached = [];

    extension(FastString buffer)
    {
        public StringWatch Watch(Func<object> onWatch, Func<string> onModify, bool resetWatch = false)
        {
            if (!_cached.TryGetValue(buffer, out var watch) || resetWatch) {
                watch = _cached[buffer] = new(onWatch, onModify, buffer);
            }

            return watch;
        }

        public string With(string value)
        {
            return _cached.TryGetValue(buffer, out var watch) ? watch.With(value) : value;
        }
    }

    public record StringWatch(Func<object> OnWatch, Func<string> OnModify, FastString Buffer)
    {
        public override string ToString()
        {
            if (Buffer.IsModified(OnWatch())) {
                Buffer.Set(OnModify());
            }

            return Buffer.ToString();
        }

        public string With(string value)
        {
            return this + value;
        }
    }
}