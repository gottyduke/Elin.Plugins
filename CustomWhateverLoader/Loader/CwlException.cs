using System.Diagnostics;
using Cwl.Helper.Unity;
using Cwl.ThirdParty;
using UnityEngine;

namespace Cwl;

internal partial class CwlMod
{
    [Conditional("DEBUG")]
    private static void SetupExceptionHook()
    {
        Glance.TryConnect();

        Application.logMessageReceived += ExceptionHandler;
    }

    private static void ExceptionHandler(string message, string stackTrace, LogType type)
    {
        if (type is not LogType.Exception) {
            return;
        }

        if (message.IsEmpty()) {
            return;
        }

        if (CwlConfig.LoggingExceptionPopup) {
            using var progress = ProgressIndicator.CreateProgressScoped(() => new(message));
        }

        if (CwlConfig.LoggingExceptionAnalyze && !stackTrace.IsEmpty()) {
        }
    }
}