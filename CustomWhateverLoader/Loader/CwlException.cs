using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using UnityEngine;

namespace Cwl;

internal partial class CwlMod
{
    private static void SetupExceptionHook()
    {
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

        var profile = ExceptionProfile.GetFromStackTrace(stackTrace, message);
        if (profile.Hidden) {
            return;
        }

        if (!CwlConfig.ExceptionPopup) {
            return;
        }

        profile.CreateAndPop(message.TruncateAllLines(125));
    }
}