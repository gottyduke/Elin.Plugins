using Emmersive.Helper;
using UnityEngine;

namespace Emmersive.Components;

public partial class EmScheduler
{
    public static bool IsBuffering { get; private set; }
    public static float NextBufferFlush { get; private set; }
    public static bool BufferReady => IsBuffering && Time.unscaledTime >= NextBufferFlush;

    public static void AddBufferDelay(float seconds)
    {
        NextBufferFlush += seconds;
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

        try {
            if (Mode == SchedulerMode.Stop) {
                foreach (var trigger in _buffer) {
                    trigger.Chara.Profile.LockedInRequest = false;
                }
            } else {
                RequestScenePlayWithTrigger(_buffer.ToArray());
            }
        } finally {
            _buffer.Clear();
        }
    }
}