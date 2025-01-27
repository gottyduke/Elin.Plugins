using System;
using System.Collections.Generic;

namespace Cwl.Helper.String;

public static class FastStringWatch
{
    private static readonly Dictionary<FastString, StringWatch> _cached = [];

    public static FastString Watch(this FastString buffer, Func<string> onWatch, Func<string> onModify, bool resetWatch = false)
    {
        if (!_cached.ContainsKey(buffer) || resetWatch) {
            _cached[buffer] = new(onWatch, onModify);
        }

        return buffer;
    }

    public static string With(this FastString buffer, string value)
    {
        if (!_cached.TryGetValue(buffer, out var watch)) {
            return value;
        }

        if (buffer.IsModified(watch.OnWatch())) {
            buffer.Set(watch.OnModify());
        }

        return buffer + value;
    }

    private record StringWatch(Func<string> OnWatch, Func<string> OnModify);
}