using System;
using Emmersive.Helper;
using UnityEngine;

namespace Emmersive.Components;

public partial class EmScheduler
{
    public enum ScheduleBufferMode
    {
        UniqueFrame,
        Incremental,
    }

    private static int _frameCount;

    public static ScheduleBufferMode BufferMode { get; set; } = ScheduleBufferMode.Incremental;
    public static bool IsBuffering { get; private set; }
    public static float NextBufferFlush { get; private set; }
    public static bool BufferReady => IsBuffering && Time.unscaledTime >= NextBufferFlush;

    public static void AddBufferDelay(float seconds)
    {
        switch (BufferMode) {
            case ScheduleBufferMode.Incremental:
                NextBufferFlush += seconds;
                break;
            case ScheduleBufferMode.UniqueFrame:
                if (_frameCount != core.frame) {
                    NextBufferFlush += seconds;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _frameCount = core.frame;
    }

    private static void AddToBuffer(SceneTriggerEvent trigger)
    {
        _buffer.Add(trigger);

        trigger.Chara.Profile.LockedInRequest = true;

        if (IsBuffering) {
            return;
        }

        IsBuffering = true;

        NextBufferFlush = Mathf.Max(NextBufferFlush, Time.unscaledTime);
    }

    private static void FlushBuffer()
    {
        IsBuffering = false;

        if (_buffer.Count == 0) {
            return;
        }

        if (Mode != SchedulerMode.Stop) {
            RequestScenePlayWithTrigger();
        }

        _buffer.Clear();
    }
}