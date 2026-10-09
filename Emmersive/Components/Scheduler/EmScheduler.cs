using System.Collections.Generic;
using System.Threading;
using Emmersive.API.Services;
using Emmersive.LangMod;
using ReflexCLI.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    private const string EnabledChunk = "scheduler_enabled";

    internal static SemaphoreSlim Semaphore = null!;
    private static readonly List<SceneTriggerEvent> _buffer = [];
    private static SchedulerMode _modeBeforeDryRun = SchedulerMode.Buffer;
    private static SchedulerMode _modeBeforeStop = SchedulerMode.Buffer;

    public static float ScenePlayDelay { get; private set; }
    public static bool IsInProgress { get; private set; }
    public static SchedulerMode Mode { get; private set; } = SchedulerMode.Buffer;
    public static float GlobalCooldown { get; private set; }

    public static bool IsEnabled => Mode is SchedulerMode.Buffer or SchedulerMode.Immediate;

    public static bool CanMakeRequest =>
        !IsBusy &&
        IsEnabled &&
        GlobalCooldown <= 0f;

    internal static bool IsBusy => IsInProgress || Semaphore.CurrentCount == 0;

    private static bool IsTyping =>
        EventSystem.current is { currentSelectedGameObject: { } selected } &&
        selected.GetComponent<InputField>() is { isFocused: true };

    private void Awake()
    {
        if (ResourceFetch.Context.Load<bool>(EnabledChunk, out var enabled) && !enabled) {
            Mode = SchedulerMode.Stop;
        }
    }

    private void Update()
    {
        var toggleKey = EmConfig.Policy.ToggleKey.Value;
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey) && !IsTyping) {
            Toggle();
        }

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

        EmMod.Log<EmScheduler>($"switching scheduling mode {Mode} -> {mode}");

        if (mode == SchedulerMode.DryRun) {
            _modeBeforeDryRun = Mode;
        }

        Mode = mode;

        if (mode != SchedulerMode.DryRun) {
            ResourceFetch.Context.SaveUncompressed(EnabledChunk, IsEnabled);
        }
    }

    public static void Toggle()
    {
        if (Mode == SchedulerMode.DryRun) {
            return;
        }

        if (IsEnabled) {
            _modeBeforeStop = Mode;
            SwitchMode(SchedulerMode.Stop);
        } else {
            SwitchMode(_modeBeforeStop);
        }

        EmMod.Popup<EmScheduler>("em_ui_scheduler_toggle".Loc((IsEnabled ? "on" : "off").lang()));
    }

    internal static void RestoreFromDryRun()
    {
        if (Mode == SchedulerMode.DryRun) {
            SwitchMode(_modeBeforeDryRun);
        }
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