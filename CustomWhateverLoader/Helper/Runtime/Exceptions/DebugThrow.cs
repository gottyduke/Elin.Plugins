using System;
using System.Diagnostics;

namespace Cwl.Helper.Exceptions;

public static class DebugThrow
{
    [Conditional("DEBUG")]
    public static void Void(Exception ex)
    {
#if DEBUG
        throw ex;
#endif
    }

    public static T Return<T>(Exception ex, T value)
    {
#if DEBUG
        throw ex;
#else
        return value;
#endif
    }
}