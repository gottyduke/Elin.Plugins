using System.Collections.Generic;
using Cwl.LangMod;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Emmersive.Components;

[ConsoleCommandClassCustomizer("em")]
public partial class EmScheduler : EMono
{
    public enum ScheduleMode
    {
        Immediate,
        Buffer,
        Stop,
        DryRun,
    }

    private static readonly List<SceneTriggerEvent> _buffer = [];

    private static float _bufferStartTime = -1f;
    private static bool _isBuffering;
    private static float _sceneDelay;
    public static bool IsInProgress { get; private set; }
    public static ScheduleMode Mode { get; private set; } = ScheduleMode.Buffer;
    public static bool BufferReady => _isBuffering && Time.time - _bufferStartTime >= EmConfig.Scene.SceneTriggerWindow.Value;

    private void Update()
    {
        if (Mode == ScheduleMode.Immediate || BufferReady) {
            FlushBuffer();
        }

        if (_sceneDelay > 0f) {
            _sceneDelay -= Time.deltaTime;
            IsInProgress = true;
        } else {
            IsInProgress = false;
        }
    }

    public static void SwitchMode(ScheduleMode mode)
    {
        if (Mode == mode) {
            return;
        }

        EmMod.Log<EmScheduler>("em_ui_switch_scheduling".Loc(Mode, mode));

        Mode = mode;
    }

    public static void SetScenePlayDelay(float seconds)
    {
        _sceneDelay = seconds;
    }

    public static void OnTalkTrigger(SceneTriggerEvent trigger)
    {
        AddToBuffer(trigger);
    }

    private static void AddToBuffer(SceneTriggerEvent trigger)
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