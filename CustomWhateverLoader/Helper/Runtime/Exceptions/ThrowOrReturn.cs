using System;

namespace Cwl.Helper.Exceptions;

public static class ThrowOrReturn
{
    public static void NoReturn(Exception ex)
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