using System;

namespace Cwl.Helper;

public struct ScopeExit : IDisposable
{
    public bool Alive { get; private set; }

    public void Dispose()
    {
        Alive = false;
    }
}