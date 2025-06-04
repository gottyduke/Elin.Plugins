using System.Collections.Generic;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    private static readonly Stack<object> _stack = [];

    public static void Push(object value)
    {
        _stack.Push(value);
    }

    public static T Pop<T>()
    {
        if (_stack.Count > 0) {
            return (T)_stack.Pop();
        }

        return default!;
    }

    public static void Clear()
    {
        _stack.Clear();
    }
}