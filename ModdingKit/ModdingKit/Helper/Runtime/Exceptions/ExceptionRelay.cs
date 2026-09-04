using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EModding.Helper.Runtime.Exceptions;

internal static class ExceptionRelay
{
    private const int Capacity = 8;

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    private static readonly object _lock = new();
    private static readonly List<(string trace, string message, Exception exception)> _pending = [with(Capacity)];

    private static bool _installed;

    public static bool Active { get; private set; }
    public static string Status { get; private set; } = "not installed";

    public static void Install()
    {
        if (_installed) {
            return;
        }

        _installed = true;

        try {
            var target = AccessTools.DeclaredMethod(typeof(StackTraceUtility), "ExtractStringFromExceptionInternal");
            if (target is null) {
                Status = "target missing";
                Debug.Log("#modding-kit exception relay target missing, falling back to string parsing");
                return;
            }

            new Harmony($"{ModInfo.Guid}.relay").Patch(
                target,
                postfix: new(AccessTools.DeclaredMethod(typeof(ExceptionRelay), nameof(Capture))));

            Active = true;
            Status = "active";
        } catch (Exception ex) {
            Status = $"{ex.GetType().Name}: {ex.Message}";
            Debug.Log($"#modding-kit exception relay unavailable, falling back to string parsing: {ex}");
            // noexcept
        }
    }

    public static Exception? Take(string stackTrace, string message)
    {
        lock (_lock) {
            var index = _pending.FindIndex(e => e.trace == stackTrace);
            if (index < 0 && !string.IsNullOrEmpty(message)) {
                index = _pending.FindIndex(e => e.message == message);
            }

            if (index < 0) {
                return null;
            }

            var exception = _pending[index].exception;
            _pending.RemoveAt(index);
            return exception;
        }
    }

    // https://github.com/Unity-Technologies/UnityCsReference/blob/2021.3/Runtime/Export/Scripting/StackTrace.cs#L58
    private static void Capture(object exceptiono, ref string message, ref string stackTrace)
    {
        if (exceptiono is not Exception exception) {
            return;
        }

        lock (_lock) {
            if (_pending.Count >= Capacity) {
                _pending.RemoveAt(0);
            }

            _pending.Add((stackTrace, message, exception));
        }
    }
}