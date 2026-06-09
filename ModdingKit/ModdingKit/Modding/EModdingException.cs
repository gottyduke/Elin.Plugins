using EModding.Helper;
using EModding.Helper.Runtime.Exceptions;
using UnityEngine;

namespace EModding;

internal partial class EModdingKit
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

        if (string.IsNullOrWhiteSpace(message) || message == "InvalidOperationException: Steamworks is not initialized.") {
            return;
        }

        var profile = ExceptionProfile.GetFromStackTrace(stackTrace, message);
        if (profile.Hidden) {
            return;
        }

        if (!Core.Instance.config.other.exceptionPopup) {
            return;
        }

        profile.CreateAndPop(message.TruncateAllLines(125));
    }
}