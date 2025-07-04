using System.Runtime.CompilerServices;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using UnityEngine;

namespace ElinPad;

internal sealed partial class ElinPad
{
    private static readonly Color _warningColor = new(237f / 255, 96f / 255, 71f / 255);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Log(object payload)
    {
        LogInternal($"[ElinPad][INFO] {payload}");
    }

    internal static void Log<T>(object payload)
    {
        Log($"[{typeof(T).Name}] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Debug(object payload, [CallerMemberName] string caller = "")
    {
        if (!PadConfig.LoggingVerbose) {
            return;
        }

        LogInternal($"[ElinPad][DEBUG] [{caller}] {payload}");
    }

    internal static void Debug<T>(object payload, [CallerMemberName] string caller = "")
    {

        Debug($"[{typeof(T).Name}] {payload}", caller);
    }

    [SwallowExceptions]
    internal static void Warn(object payload)
    {
        LogInternal($"[ElinPad][WARN] {payload}");
    }

    internal static void Warn<T>(object payload)
    {
        Warn($"[{typeof(T).Name}] {payload}");
    }

    internal static void WarnWithPopup<T>(object payload, object? log = null)
    {
        Warn<T>(payload);
        using var progress = ProgressIndicator.CreateProgressScoped(() => new(payload.ToTruncateString(150)));

        if (log is null) {
            return;
        }

        LogInternal(log);
        progress.Get<ProgressIndicator>().AppendHoverText(() => log.ToTruncateString(450).TruncateAllLines(150));
    }

    [SwallowExceptions]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Error(object payload, [CallerMemberName] string caller = "")
    {
        LogInternal($"[ElinPad][ERROR] [{caller}] {payload}");
    }

    internal static void Error<T>(object payload, [CallerMemberName] string caller = "")
    {
        Error($"[{typeof(T).Name}] {payload}", caller);
    }

    internal static void ErrorWithPopup<T>(object payload, object? log = null, [CallerMemberName] string caller = "")
    {
        Error<T>(payload, caller);
        using var progress =
            ProgressIndicator.CreateProgressScoped(() => new(payload.ToTruncateString(150), Color: _warningColor));

        if (log is null) {
            return;
        }

        LogInternal(log);
        progress.Get<ProgressIndicator>().AppendHoverText(() => log.ToTruncateString(450).TruncateAllLines(150));
    }

    private static void LogInternal(object log)
    {
        UnityEngine.Debug.Log(log.RemoveColorTag());
    }
}