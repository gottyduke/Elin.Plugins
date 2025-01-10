#if !DEBUG
global using SwallowExceptions.Fody;
#endif

using System;
using Cwl.ThirdParty;
using UnityEngine;

namespace Cwl;

#if DEBUG
internal class SwallowExceptions : Attribute;
#endif

internal partial class CwlMod
{
    private void SetupExceptionHook()
    {
        Glance.TryConnect();

        Application.logMessageReceived += ExceptionHandler;
    }

    private static void ExceptionHandler(string message, string stackTrace, LogType type)
    {
        if (type is not (LogType.Error or LogType.Exception)) {
            return;
        }

        if (message is null or "" || stackTrace is null or "") {
        }
    }
}