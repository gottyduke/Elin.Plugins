using System;
using System.Collections.Generic;
using System.Linq;
using ElinPad.API;
using ElinPad.API.Event;
using ElinPad.Implementation.Event;
using ElinPad.Native;
using UnityEngine;

namespace ElinPad.Components;

public class PadEventManager : EMono
{
    private static readonly Dictionary<GamepadButton, PadButtonState> _buttonStates =
        Enum.GetValues(typeof(GamepadButton))
            .OfType<GamepadButton>()
            .ToDictionary(bf => bf, _ => new PadButtonState());

    private static readonly Dictionary<PadAxisEventType, PadAxisState> _axisStates =
        Enum.GetValues(typeof(PadAxisEventType))
            .OfType<PadAxisEventType>()
            .ToDictionary(af => af, _ => new PadAxisState());

    public static Gamepad MainPad => PadController.MainPad;

    public static event EventHandler<PadButtonEventArgs> OnPadButtonEvent = delegate { };
    public static event EventHandler<PadAxisEventArgs> OnPadAxisEvent = delegate { };

    internal static void Dispatch(GamepadState pad, uint packetNumber = unchecked((uint)-1))
    {
        var currentTime = Time.time;
        var doubleTapThreshold = EpConfig.DoubleTapThreshold;
        var holdThreshold = EpConfig.HoldThreshold;

        foreach (var (button, state) in _buttonStates) {
            var isDown = pad.GetButton(button);
            state.WasDown = state.IsDown;
            state.IsDown = isDown;

            switch (isDown) {
                case true when !state.WasDown: {
                    state.DownTime = currentTime;
                    ProcessButton(pad, button, PadButtonEventType.Down);

                    if (currentTime - state.LastPressTime < doubleTapThreshold) {
                        ProcessButton(pad, button, PadButtonEventType.DoublePress);
                        state.IsPressed = false;
                    }

                    break;
                }
                case false when state.WasDown: {
                    var duration = currentTime - state.DownTime;
                    ProcessButton(pad, button, PadButtonEventType.Up, duration);

                    if (duration < holdThreshold && !state.IsPressed) {
                        state.LastPressTime = currentTime;
                        ProcessButton(pad, button, PadButtonEventType.Press);
                    }

                    break;
                }
                case true when state.WasDown: {
                    var duration = currentTime - state.DownTime;
                    if (duration > holdThreshold) {
                        ProcessButton(pad, button, PadButtonEventType.Hold, duration);
                    }

                    break;
                }
            }
        }

        const float axisChangeThreshold = 0.01f;

        foreach (var (axis, state) in _axisStates) {
            var currentValue = axis switch {
                PadAxisEventType.LeftStick => pad.LeftStickAxes,
                PadAxisEventType.RightStick => pad.RightStickAxes,
                PadAxisEventType.LeftTrigger => new(pad.LeftTriggerAxis, pad.LeftTriggerAxis),
                PadAxisEventType.RightTrigger => new(pad.RightTriggerAxis, pad.RightTriggerAxis),
                _ => Vector2.zero,
            };

            var delta = currentValue - state.Value;
            if (delta.sqrMagnitude <= axisChangeThreshold * axisChangeThreshold) {
                continue;
            }

            state.LastValue = state.Value;
            state.Value = currentValue;

            ProcessAxis(pad, axis, currentValue, delta);
        }
    }

    private static void ProcessButton(GamepadState pad, GamepadButton button, PadButtonEventType type, float duration = 0f)
    {
        OnPadButtonEvent.Invoke(MainPad, new(type, button, duration) {
            Gamepad = pad,
        });

        ElinPad.Debug<PadEventManager>($"dispatch btn {button} {type} time {duration * 1000:000}ms");
    }

    private static void ProcessAxis(GamepadState pad, PadAxisEventType type, Vector2 value, Vector2 delta)
    {
        OnPadAxisEvent.Invoke(MainPad, new(type, value, delta) {
            Gamepad = pad,
        });

        ElinPad.Debug<PadEventManager>($"dispatch axis {type} {value} delta {delta}");
    }
}