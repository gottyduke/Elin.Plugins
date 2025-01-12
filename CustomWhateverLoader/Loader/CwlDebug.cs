﻿#if !DEBUG
global using SwallowExceptions.Fody;
#endif

#if DEBUG
using Cwl.ThirdParty;
using UnityEngine;
using System;

namespace Cwl;

internal class SwallowExceptions : Attribute;

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

        if (message.IsEmpty() || stackTrace.IsEmpty()) {
        }
    }
}
#endif