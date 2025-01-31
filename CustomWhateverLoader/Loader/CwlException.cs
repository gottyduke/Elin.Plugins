using System.Collections.Generic;
using Cwl.Helper.Runtime.Exceptions;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.ThirdParty;
using UnityEngine;

namespace Cwl;

internal partial class CwlMod
{
    private static readonly Dictionary<int, ProgressIndicator> _activeExceptions = [];

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

        if (!CwlConfig.LoggingExceptionPopup) {
            return;
        }

        EMono.ui?.hud?.imageCover?.SetActive(false);

        if (_activeExceptions.TryGetValue(profile.Key, out var progress)) {
            if (progress != null) {
                progress
                    .AppendTailText(() => profile.Occurrences <= 999 ? profile.Occurrences.ToString() : "999+")
                    .ResetProgress();
                return;
            }
        }

        using var scopeExit = ProgressIndicator.CreateProgressScoped(() => new(message, Color: Color.red));
        _activeExceptions[profile.Key] = progress = scopeExit.Get<ProgressIndicator>();

        if (!CwlConfig.LoggingExceptionAnalyze) {
            return;
        }

        progress
            .AppendHoverText(() => {
                profile.StartAnalyzing();
                return profile.Result;
            })
            .SetHoverPrompt("cwl_ui_exception_analyzing".Loc(), "cwl_ui_exception_analyze".Loc());
        CoroutineHelper.Deferred(
            () => {
                if (progress == null) {
                    return;
                }

                progress.SetHoverPrompt();
            },
            () => progress == null || profile.Analyzed);
    }
}