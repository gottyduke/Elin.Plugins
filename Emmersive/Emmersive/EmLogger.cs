using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using UnityEngine;

namespace Emmersive;

internal sealed partial class EmMod
{
    private static readonly Color _warningColor = new(237f / 255, 96f / 255, 71f / 255);
    private static readonly ConcurrentQueue<string> _logs = [];

    private void Update()
    {
        while (_logs.TryDequeue(out var log)) {
            UnityEngine.Debug.Log(log);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Log(object? payload)
    {
        LogInternal($"[EMME][INFO] {payload}");
    }

    internal static void Log<T>(object? payload)
    {
        Log($"[{typeof(T).Name}] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Debug(object? payload, [CallerMemberName] string caller = "")
    {
        if (!EmConfig.Policy.Verbose.Value) {
            return;
        }

        LogInternal($"[EMME][DEBUG] [{caller}] {payload}");
    }

    internal static void Debug<T>(object? payload, [CallerMemberName] string caller = "")
    {
        Debug($"[{typeof(T).Name}] {payload}", caller);
    }

    internal static void Trace<T>(object? payload, [CallerMemberName] string caller = "")
    {
        Debug($"[{typeof(T).Name}] {payload}", caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Warn(object? payload)
    {
        LogInternal($"[EMME][WARN] {payload}");
    }

    internal static void Warn<T>(object? payload)
    {
        Warn($"[{typeof(T).Name}] {payload}");
    }

    internal static void WarnWithPopup<T>(object payload, object? log = null)
    {
        Warn<T>(payload);

        switch (log) {
            case Exception ex:
                var exp = ExceptionProfile.GetFromStackTrace(ref ex);
                exp.CreateAndPop(payload.ToString());
                break;
            default: {
                LogInternal(log ?? payload);
                using var progress = ProgressIndicator.CreateProgressScoped(() => new(payload.ToTruncateString(150)));
                if (log is not null) {
                    progress.Get<ProgressIndicator>()
                        .OnHover(p => GUILayout.Label(log.ToTruncateString(450).TruncateAllLines(150), p.GUIStyle));
                }

                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Error(object? payload, [CallerMemberName] string caller = "")
    {
        LogInternal($"[CWL][ERROR] [{caller}] {payload}");
    }

    internal static void Error<T>(object? payload, [CallerMemberName] string caller = "")
    {
        Error($"[{typeof(T).Name}] {payload}", caller);
    }

    internal static void ErrorWithPopup<T>(object payload, object? log = null, [CallerMemberName] string caller = "")
    {
        Error<T>(payload, caller);

        switch (log) {
            case Exception ex:
                var exp = ExceptionProfile.GetFromStackTrace(ref ex);
                exp.CreateAndPop(payload.ToString());
                break;
            default: {
                LogInternal(log ?? payload);
                using var progress = ProgressIndicator
                    .CreateProgressScoped(() => new(payload.ToTruncateString(150), Color: _warningColor));
                if (log is not null) {
                    progress.Get<ProgressIndicator>()
                        .OnHover(p => GUILayout.Label(log.ToTruncateString(450).TruncateAllLines(150), p.GUIStyle));
                }

                break;
            }
        }
    }

    [Conditional("DEBUG")]
    internal static void DebugPopup<T>(string message, float seconds = 2.5f)
    {
        Popup<T>(message, seconds);
    }

    internal static void Popup<T>(string message, float seconds = 2.5f)
    {
        Log<T>(message);
        using var progress = ProgressIndicator.CreateProgressScoped(() => new(message), seconds);
    }

    private static void LogInternal(object log)
    {
        _logs.Enqueue(log.RemoveTagColor());
    }
}