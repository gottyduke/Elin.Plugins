using System.Runtime.CompilerServices;
using Cwl.Helper.Unity;
using Cwl.ThirdParty;
using UnityEngine;

namespace Cwl;

internal sealed partial class CwlMod
{
    private static readonly Color _warningColor = new(237f / 255, 96f / 255, 71f / 255);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Log(object payload)
    {
        UnityEngine.Debug.Log($"[CWL][INFO] {payload}");
    }

    internal static void Log<T>(object payload)
    {
        Log($"[{typeof(T).Name}] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Debug(object payload, [CallerMemberName] string caller = "")
    {
        UnityEngine.Debug.Log($"[CWL][DEBUG] [{caller}] {payload}");
    }

    internal static void Debug<T>(object payload, [CallerMemberName] string caller = "")
    {
        if (!CwlConfig.LoggingVerbose) {
            return;
        }

        Debug($"[{typeof(T).Name}] {payload}", caller);
    }

    [SwallowExceptions]
    internal static void Warn(object payload)
    {
        UnityEngine.Debug.Log($"[CWL][WARN] {payload}");
        Glance.Dispatch(payload);
    }

    internal static void Warn<T>(object payload)
    {
        Warn($"[{typeof(T).Name}] {payload}");
    }

    internal static void WarnWithPopup<T>(object payload, object? log = null)
    {
        Warn<T>(payload);
        using var progress = ProgressIndicator.CreateProgressScoped(() => new(payload.ToString()));

        if (log is null) {
            return;
        }

        UnityEngine.Debug.Log(log);
        progress.Get<ProgressIndicator>().AppendHoverText(log.ToString);
    }

    [SwallowExceptions]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Error(object payload, [CallerMemberName] string caller = "")
    {
        UnityEngine.Debug.Log($"[CWL][ERROR] [{caller}] {payload}");
        Glance.Dispatch(payload);
    }

    internal static void Error<T>(object payload, [CallerMemberName] string caller = "")
    {
        Error($"[{typeof(T).Name}] {payload}", caller);
    }

    internal static void ErrorWithPopup<T>(object payload, object? log = null, [CallerMemberName] string caller = "")
    {
        Error<T>(payload, caller);
        using var progress = ProgressIndicator.CreateProgressScoped(() => new(payload.ToString(), Color: _warningColor));

        if (log is null) {
            return;
        }

        UnityEngine.Debug.Log(log);
        progress.Get<ProgressIndicator>().AppendHoverText(log.ToString);
    }
}