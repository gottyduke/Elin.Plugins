using System;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.ThirdParty;
using UnityEngine;

namespace Cwl;

internal partial class CwlMod
{
    private static void SetupExceptionHook()
    {
        Glance.TryConnect();
        Application.logMessageReceived += ExceptionHandler;
    }

    private static void ExceptionHandler(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Exception) {
            return;
        }

        if (message.IsEmpty() || message == "InvalidOperationException: Steamworks is not initialized.") {
            return;
        }

        var profile = ExceptionProfile.GetFromStackTrace(stackTrace);
        if (profile.Hidden) {
            return;
        }

        if (!CwlConfig.ExceptionPopup) {
            return;
        }

        // missing method exception
        if (message.StartsWith(nameof(MissingMethodException))) {
            message = "cwl_warn_missing_method".Loc(message);
        }

        profile.CreateAndPop(message.TruncateAllLines(125));
    }
}