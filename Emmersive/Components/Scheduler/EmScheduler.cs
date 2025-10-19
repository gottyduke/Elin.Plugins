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
    private static int _iteration;

    public static float ScenePlayDelay { get; private set; }
    public static bool IsInProgress { get; private set; }
    public static ScheduleMode Mode { get; private set; } = ScheduleMode.Buffer;
    public static bool CanMakeRequest => !IsInProgress || _iteration < EmConfig.Policy.ConcurrentRequests.Value;

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
}