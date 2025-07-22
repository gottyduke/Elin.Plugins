using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwl.Helper.Unity;

public static class DeferredCoroutine
{
    private static readonly List<(Action action, Func<bool> condition)> _deferredAwaiters = [];
    private static Coroutine? _awaiter;
    private static bool _halt;

    public static void StartDeferredCoroutine(this MonoBehaviour instance, Action action, int frames = 1)
    {
        instance.StartCoroutine(DeferredFrames(action, frames));
    }

    public static void StartDeferredCoroutine(this MonoBehaviour instance, Action action, float seconds)
    {
        instance.StartCoroutine(DeferredSeconds(action, seconds));
    }

    public static void StartDeferredCoroutine(this MonoBehaviour instance, Action action, Func<bool> condition)
    {
        _halt = false;
        _deferredAwaiters.Add((action, condition));
        _awaiter ??= instance.StartCoroutine(DeferredAwaiter());
    }

    private static IEnumerator DeferredFrames(Action action, int frames = 1)
    {
        if (frames == 1) {
            Core.Instance.actionsNextFrame.Add(action);
        } else {
            for (var i = 0; i < frames; ++i) {
                yield return null;
            }

            action();
        }
    }

    private static IEnumerator DeferredSeconds(Action action, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        action();
    }

    private static IEnumerator DeferredAwaiter()
    {
        while (!_halt) {
            for (var i = _deferredAwaiters.Count - 1; i >= 0; --i) {
                var (action, condition) = _deferredAwaiters[i];
                if (!condition()) {
                    continue;
                }

                action();
                _deferredAwaiters.RemoveAt(i);
            }

            if (_deferredAwaiters.Count == 0) {
                _awaiter = null;
                yield break;
            }

            yield return null;
        }
    }
}