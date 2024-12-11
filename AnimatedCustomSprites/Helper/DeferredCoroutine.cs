using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ACS.Helper;

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
        _awaiter ??= instance.StartCoroutine(DeferredAwaiter(1));
        _deferredAwaiters.Add((action, condition));
    }

    private static IEnumerator DeferredFrames(Action action, int frames = 1)
    {
        for (var i = 0; i < frames; ++i) {
            yield return null;
        }

        action();
    }

    private static IEnumerator DeferredSeconds(Action action, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        action();
    }

    private static IEnumerator DeferredAwaiter(int frames)
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

            for (var i = 0; i < frames; ++i) {
                yield return null;
            }
        }
    }
}