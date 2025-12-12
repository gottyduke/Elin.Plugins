using System.Collections.Generic;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    private static readonly Stack<object> _valueStack = [];

    public static void Push(object value)
    {
        _valueStack.Push(value);
    }

    public static T Pop<T>()
    {
        if (_valueStack.Count > 0) {
            return (T)_valueStack.Pop();
        }

        return default!;
    }
}