using System.Collections.Generic;
using System.Threading;
using Emmersive.API.Plugins.SceneScheduler;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Emmersive.Components;

[ConsoleCommandClassCustomizer("em")]
public partial class EmScheduler : EMono
{
    private static readonly List<SceneTriggerEvent> _buffer = [];
    private static CancellationTokenSource? _sceneCts;

    private static float _bufferStartTime = -1f;
    private static bool _isBuffering;
    private static float _sceneDelay;
    public static bool IsInProgress { get; private set; }

    private void Update()
    {
        if (_isBuffering && Time.time - _bufferStartTime >= EmConfig.Scene.SceneTriggerWindow.Value) {
            FlushBuffer();
        }

        if (_sceneDelay > 0f) {
            _sceneDelay -= Time.deltaTime;
            IsInProgress = true;
        } else {
            IsInProgress = false;
        }
    }

    public static void SetScenePlayDelay(float seconds)
    {
        _sceneDelay = seconds;
    }

    public static void OnTalkTrigger(SceneTriggerEvent trigger)
    {
        _buffer.Add(trigger);

        if (_isBuffering) {
            return;
        }

        _isBuffering = true;
        _bufferStartTime = Time.time;
    }

    private static void FlushBuffer()
    {
        _isBuffering = false;

        if (_buffer.Count == 0) {
            return;
        }

        RequestScenePlayWithTrigger();

        _buffer.Clear();
    }
}