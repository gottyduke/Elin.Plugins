using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cwl.Helper.Unity;

public class CoroutineHelper : MonoBehaviour
{
    [field: AllowNull]
    private static CoroutineHelper Instance => field ??= CwlMod.Instance.GetOrCreate<CoroutineHelper>();

    public static Coroutine Immediate(IEnumerator co)
    {
        return Instance.StartCoroutine(co);
    }

    public static void Immediate(Action action)
    {
        Instance.StartDeferredCoroutine(action, 0);
    }

    /// <summary>
    ///     Defer for frames
    /// </summary>
    /// <param name="action"></param>
    /// <param name="frames"></param>
    public static void Deferred(Action action, int frames = 1)
    {
        if (frames == 1) {
            Core.Instance.actionsNextFrame.Add(action);
        } else {
            Instance.StartDeferredCoroutine(action, frames);
        }
    }

    /// <summary>
    ///     Defer for seconds
    /// </summary>
    public static void Deferred(Action action, float seconds)
    {
        if (seconds == 0f) {
            Core.Instance.actionsNextFrame.Add(action);
        } else {
            Instance.StartDeferredCoroutine(action, seconds);
        }
    }

    /// <summary>
    ///     Defer unless true
    /// </summary>
    public static void Deferred(Action action, Func<bool> condition)
    {
        Instance.StartDeferredCoroutine(action, condition);
    }

    public static void Halt(Coroutine coroutine)
    {
        Instance.StopCoroutine(coroutine);
    }

    // No
    internal static void HaltAll()
    {
        Instance.StopAllCoroutines();
    }
}