using System;

namespace Cwl.Helper;

public class ScopeExit : IDisposable
{
    public bool Alive { get; private set; } = true;
    public object? Object { private get; set; }
    public Action? OnExit { get; set; }

    public void Dispose()
    {
        Alive = false;
        OnExit?.Invoke();
    }

    public T Get<T>() where T : class
    {
        return Object as T ?? throw new InvalidCastException(nameof(ScopeExit), new NullReferenceException(nameof(Object)));
    }
}