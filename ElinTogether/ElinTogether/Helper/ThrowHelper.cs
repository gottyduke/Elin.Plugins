using System;
using System.Diagnostics.CodeAnalysis;
using Serilog.Core;

#pragma warning disable CA2254

namespace ElinTogether.Helper;

internal class ThrowHelper
{
    [DoesNotReturn]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Throw(Exception ex, string messageTemplate, params object?[]? args)
    {
        EmpLog.Fatal(ex, messageTemplate, args);
        throw ex;
    }
}