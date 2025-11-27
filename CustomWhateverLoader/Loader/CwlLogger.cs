using System;
using System.Runtime.CompilerServices;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using UnityEngine;

namespace Cwl;

internal sealed partial class CwlMod
{
    private static readonly Color _warningColor = new(237f / 255, 96f / 255, 71f / 255);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Log(object payload)
    {
        LogInternal($"[CWL][INFO] {payload}");
    }

    internal static void Log<T>(object payload)
    {
        Log($"[{typeof(T).Name}] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Debug(object payload, [CallerMemberName] string caller = "")
    {
        if (!CwlConfig.LoggingVerbose) {
            return;
        }

        LogInternal($"[CWL][DEBUG] [{caller}] {payload}");
    }

    internal static void Debug<T>(object payload, [CallerMemberName] string caller = "")
    {
        Debug($"[{typeof(T).Name}] {payload}", caller);
    }

    internal static void Warn(object payload)
    {
        LogInternal($"[CWL][WARN] {payload}");
    }

    internal static void Warn<T>(object payload)
    {
        Warn($"[{typeof(T).Name}] {payload}");
    }

    // not thread safe
    internal static void WarnWithPopup<T>(object payload, object? log = null)
    {
        Warn<T>(payload);

        switch (log) {
            case Exception ex:
                var exp = ExceptionProfile.GetFromStackTrace(ref ex);
                exp.CreateAndPop(payload.ToString());
                break;
            default: {
                PopupInternal(payload.RemoveTagColor());
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Error(object payload, [CallerMemberName] string caller = "")
    {
        LogInternal($"[CWL][ERROR] [{caller}] {payload}");
    }

    internal static void Error<T>(object payload, [CallerMemberName] string caller = "")
    {
        Error($"[{typeof(T).Name}] {payload}", caller);
    }

    // not thread safe
    internal static void ErrorWithPopup<T>(object payload, object? log = null, [CallerMemberName] string caller = "")
    {
        Error<T>(payload, caller);

        switch (log) {
            case Exception ex:
                var exp = ExceptionProfile.GetFromStackTrace(ref ex);
                exp.CreateAndPop(payload.ToString());
                break;
            default: {
                PopupInternal(payload.RemoveTagColor(), _warningColor);
                break;
            }
        }
    }

    // not thread safe
    internal static void Popup<T>(string message)
    {
        Log<T>(message);
        PopupInternal(message);
    }

    private static void LogInternal(object log)
    {
        UnityEngine.Debug.Log(log.RemoveTagColor());
    }

    private static void PopupInternal(string log, Color? color = null)
    {
        var truncation = log.Length > 150;
        var header = log;
        if (truncation) {
            var truncated = log.ToTruncateString(150);
            if (!ReferenceEquals(truncated, header)) {
                header = truncated;
            } else {
                truncation = false;
            }
        }

        using var progress = ProgressIndicator.CreateProgressScoped(() => new(header, Color: color));

        if (truncation) {
            var footer = log.RemoveTagColor()[150..];
            progress.Get<ProgressIndicator>().OnHover(p => GUILayout.Label(footer, p.GUIStyle));
        }
    }
}