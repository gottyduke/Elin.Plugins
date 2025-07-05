using System.Collections;
using System.Collections.Generic;
using ElinPad.Native;
using UnityEngine;

namespace ElinPad.Components;

public class PadController : EMono
{
    public static readonly IReadOnlyList<Gamepad> Controllers = [
        new(GamepadIndex.One),
        new(GamepadIndex.Two),
        new(GamepadIndex.Three),
        new(GamepadIndex.Four),
    ];

    public static PadController? Instance { get; private set; }
    public static Gamepad MainPad { get; private set; } = new(GamepadIndex.One);
    public static GamepadState LastState { get; private set; }

    private void Awake()
    {
        Instance = this;

        StartPolling(0.01f);
    }

    public void StartPolling(float interval)
    {
        StopAllCoroutines();
        core.actionsNextFrame.Add(() => StartCoroutine(WaitForConnection(interval)));
    }

    public void TryReconnect()
    {
        foreach (var controller in Controllers) {
            if (!controller.IsConnected) {
                continue;
            }

            MainPad = controller;
            break;
        }
    }

    private IEnumerator UpdateState(float interval)
    {
        ElinPad.Debug<PadController>($"polling states @ {interval}s");

        var wait = new WaitForSeconds(interval);
        while (MainPad.TryGetState(out var state)) {
            if (state.PacketNumber != LastState.PacketNumber) {
                PadEventManager.Dispatch(state, state.PacketNumber);
                LastState = state;
            }

            yield return wait;
        }

        StartCoroutine(WaitForConnection(interval));
    }

    private IEnumerator WaitForConnection(float interval)
    {
        ElinPad.Debug<PadController>("waiting for connections");

        var wait = new WaitForSeconds(1f);
        while (!MainPad.IsConnected) {
            TryReconnect();

            yield return wait;
        }

        StartCoroutine(UpdateState(interval));
    }
}