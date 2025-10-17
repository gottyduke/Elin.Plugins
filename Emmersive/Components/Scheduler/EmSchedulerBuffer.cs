using System;
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

        if (Mode != ScheduleMode.Stop) {
            RequestScenePlayWithTrigger();
        }

        _buffer.Clear();
    }
}