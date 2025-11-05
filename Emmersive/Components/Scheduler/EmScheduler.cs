using System.Collections.Generic;
using System.Threading;
using Cwl.LangMod;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Emmersive.Components;

[ConsoleCommandClassCustomizer("em")]
public partial class EmScheduler : EMono
{
    public enum SchedulerMode
    {
        Immediate,
        Buffer,
        Stop,
        DryRun,
    }

    internal static SemaphoreSlim Semaphore = null!;
    private static readonly List<SceneTriggerEvent> _buffer = [];

    public static float ScenePlayDelay { get; private set; }
    public static bool IsInProgress { get; private set; }
    public static SchedulerMode Mode { get; private set; } = SchedulerMode.Buffer;
    public static float GlobalCooldown { get; private set; }

    public static bool CanMakeRequest =>
        (!IsInProgress || Semaphore.CurrentCount > 0) &&
        Mode is SchedulerMode.Buffer or SchedulerMode.Immediate &&
        GlobalCooldown <= 0f;

    private void Update()
    {
        if (Mode == SchedulerMode.Immediate || BufferReady) {
            FlushBuffer();
        }

        if (ScenePlayDelay > 0f) {
            ScenePlayDelay -= Time.deltaTime;
            IsInProgress = true;
        } else {
            IsInProgress = false;
        }

        if (GlobalCooldown > 0f) {
            GlobalCooldown -= Time.deltaTime;
        }
    }

    public static void SwitchMode(SchedulerMode mode)
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