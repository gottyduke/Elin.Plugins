using System;

namespace Cwl.Helper;

public struct ScopeExit : IDisposable
{
    public bool Alive { get; private set; }
    public object? Object { private get; set; }

    public T Get<T>() where T : class
    {
        return Object as T ?? throw new InvalidCastException(nameof(ScopeExit), new NullReferenceException(nameof(Object)));
    }

    public void Dispose()
    {
        Alive = false;
    }
}