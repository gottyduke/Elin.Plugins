using Cwl.Helper.Runtime.Exceptions;
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

        profile.CreateAndPop(message);
    }
}