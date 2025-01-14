using System.Runtime.CompilerServices;
using Cwl.ThirdParty;

namespace Cwl;

internal sealed partial class CwlMod
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Log(object payload)
    {
        UnityEngine.Debug.Log($"[CWL][INFO] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Log<T>(object payload)
    {
        Log($"[{typeof(T).Name}] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Debug(object payload, [CallerMemberName] string caller = "")
    {
        if (!CwlConfig.Logging.Verbose?.Value is true) {
            return;
        }

        UnityEngine.Debug.Log($"[CWL][DEBUG] [{caller}] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Debug<T>(object payload, [CallerMemberName] string caller = "")
    {
        if (!CwlConfig.Logging.Verbose?.Value is true) {
            return;
        }

        Debug($"[{typeof(T).Name}] {payload}", caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Warn(object payload)
    {
        UnityEngine.Debug.Log($"[CWL][WARN] {payload}");
        Glance.Dispatch(payload);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Warn<T>(object payload)
    {
        Warn($"[{typeof(T).Name}] {payload}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Error(object payload, [CallerMemberName] string caller = "")
    {
        UnityEngine.Debug.Log($"[CWL][ERROR] [{caller}] {payload}");
        Glance.Dispatch(payload);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Error<T>(object payload, [CallerMemberName] string caller = "")
    {
        Error($"[{typeof(T).Name}] {payload}", caller);
    }
}