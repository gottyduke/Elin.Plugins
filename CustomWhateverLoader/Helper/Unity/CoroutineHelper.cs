using System;
using System.Collections;
using UnityEngine;

namespace Cwl.Helper.Unity;

public class CoroutineHelper : MonoBehaviour
{
    private static readonly Lazy<CoroutineHelper> _instance = new(() => {
        if (ComponentFetch.TryFindObject<CoroutineHelper>("CoroutineHelper", out var globalHelper) &&
            globalHelper != null) {
            return globalHelper;
        }

        var go = new GameObject("CoroutineHelper");
        globalHelper = go.GetOrAddComponent<CoroutineHelper>();
        DontDestroyOnLoad(go);

        return globalHelper;
    });

    public static CoroutineHelper Instance => _instance.Value;

    public static Coroutine Immediate(IEnumerator co)
    {
        return Instance.StartCoroutine(co);
    }

    /// <summary>
    ///     Defer for frames
    /// </summary>
    /// <param name="action"></param>
    /// <param name="frames"></param>
    public static void Deferred(Action action, int frames = 1)
    {
        Instance.StartDeferredCoroutine(action, frames);
    }

    /// <summary>
    ///     Defer for seconds
    /// </summary>
    public static void Deferred(Action action, float seconds)
    {
        Instance.StartDeferredCoroutine(action, seconds);
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
    public static void HaltAll()
    {
        Instance.StopAllCoroutines();
    }
}