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
    private static int _frameCount;

    public static bool IsBuffering { get; private set; }
    public static float ScenePlayDelay { get; private set; }
    public static bool IsInProgress { get; private set; }
    public static ScheduleMode Mode { get; private set; } = ScheduleMode.Buffer;
    public static float NextBufferFlush { get; private set; }
    public static bool BufferReady => IsBuffering && Time.unscaledTime >= NextBufferFlush;

    private void Update()
    {
        if (Mode == ScheduleMode.Immediate || BufferReady) {
            FlushBuffer();
        }

        if (ScenePlayDelay > 0f) {
            ScenePlayDelay -= Time.deltaTime;
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
        ScenePlayDelay = seconds;
    }

    public static void OnTalkTrigger(SceneTriggerEvent trigger)
    {
        AddToBuffer(trigger);
    }

#region Buffer

    public static void AddBufferDelay(float seconds)
    {
        NextBufferFlush += seconds;
        _frameCount = core.frame;
    }

    public static void AddBufferDelaySameFrame(float seconds)
    {
        if (_frameCount != core.frame) {
            AddBufferDelay(seconds);
        }
    }

    private static void AddToBuffer(SceneTriggerEvent trigger)
    {
        _buffer.Add(trigger);

        if (IsBuffering) {
            return;
        }

        IsBuffering = true;

        NextBufferFlush = Mathf.Max(NextBufferFlush, Time.unscaledTime);
        AddBufferDelay(EmConfig.Scene.SceneTriggerBuffer.Value);
    }

    private static void FlushBuffer()
    {
        IsBuffering = false;

        if (_buffer.Count == 0) {
            return;
        }

        if (Mode != ScheduleMode.Stop) {
            RequestScenePlayWithTrigger();
        }

        _buffer.Clear();
    }

#endregion
}