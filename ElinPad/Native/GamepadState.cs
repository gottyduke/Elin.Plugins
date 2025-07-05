using System.Runtime.CompilerServices;
using Windows.Win32.UI.Input.XboxController;
using UnityEngine;

namespace ElinPad.Native;

public struct GamepadState(XINPUT_GAMEPAD pXInputGamepad, uint dwPacketNumber = unchecked((uint)-1))
{
    private const GamepadButton ValidButton =
        GamepadButton.DPadUp | GamepadButton.DPadDown | GamepadButton.DPadLeft | GamepadButton.DPadRight |
        GamepadButton.Menu | GamepadButton.View |
        GamepadButton.LeftStick | GamepadButton.RightStick |
        GamepadButton.LeftBumper | GamepadButton.RightBumper |
        GamepadButton.A | GamepadButton.B | GamepadButton.X | GamepadButton.Y;

    public uint PacketNumber = dwPacketNumber;

    public GamepadButton Buttons = (GamepadButton)pXInputGamepad.wButtons;
    public float LeftTriggerAxis = NormalizeTrigger(pXInputGamepad.bLeftTrigger, EpConfig.LeftTriggerDeadZone);
    public float RightTriggerAxis = NormalizeTrigger(pXInputGamepad.bRightTrigger, EpConfig.RightTriggerDeadZone);
    public float LeftStickAxisHor = NormalizeAxis(pXInputGamepad.sThumbLX, EpConfig.LeftStickDeadZone);
    public float LeftStickAxisVer = NormalizeAxis(pXInputGamepad.sThumbLY, EpConfig.LeftStickDeadZone);
    public float RightStickAxisHor = NormalizeAxis(pXInputGamepad.sThumbRX, EpConfig.RightStickDeadZone);
    public float RightStickAxisVer = NormalizeAxis(pXInputGamepad.sThumbRY, EpConfig.RightStickDeadZone);

    public bool DPadUp => GetButton(GamepadButton.DPadUp);
    public bool DPadDown => GetButton(GamepadButton.DPadDown);
    public bool DPadLeft => GetButton(GamepadButton.DPadLeft);
    public bool DPadRight => GetButton(GamepadButton.DPadRight);

    public bool Menu => GetButton(GamepadButton.Menu);
    public bool View => GetButton(GamepadButton.View);

    public bool LeftStick => GetButton(GamepadButton.LeftStick);
    public bool RightStick => GetButton(GamepadButton.RightStick);

    public bool LeftBumper => GetButton(GamepadButton.LeftBumper);
    public bool RightBumper => GetButton(GamepadButton.RightBumper);

    public bool A => GetButton(GamepadButton.A);
    public bool B => GetButton(GamepadButton.B);
    public bool X => GetButton(GamepadButton.X);
    public bool Y => GetButton(GamepadButton.Y);

    public bool LeftTrigger => LeftTriggerAxis > 0f;
    public bool RightTrigger => RightTriggerAxis > 0f;
    public Vector2 TriggerAxes => new(LeftTriggerAxis, RightTriggerAxis);

    public bool LeftStickUp => LeftStickAxisVer > 0f;
    public bool LeftStickDown => LeftStickAxisVer < 0f;
    public bool LeftStickLeft => LeftStickAxisHor < 0f;
    public bool LeftStickRight => LeftStickAxisHor > 0f;
    public Vector2 LeftStickAxes => new(LeftStickAxisHor, LeftStickAxisVer);

    public bool RightStickUp => RightStickAxisVer > 0f;
    public bool RightStickDown => RightStickAxisVer < 0f;
    public bool RightStickLeft => RightStickAxisHor < 0f;
    public bool RightStickRight => RightStickAxisHor > 0f;
    public Vector2 RightStickAxes => new(RightStickAxisHor, RightStickAxisVer);

    public bool AnyButton => GetButton(ValidButton);
    public bool AnyTrigger => LeftTrigger || RightTrigger;
    public bool AnyLeftStickAxis => LeftStickAxisHor != 0f || LeftStickAxisVer != 0f;
    public bool AnyRightStickAxis => RightStickAxisHor != 0f || RightStickAxisVer != 0f;
    public bool AnyStickAxis => AnyLeftStickAxis || AnyRightStickAxis;
    public bool AnyInput => AnyButton || AnyTrigger || AnyStickAxis;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetButton(GamepadButton button)
    {
        return (Buttons & button) != GamepadButton.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetButton(string buttonName)
    {
        return GetButton(buttonName.ToEnum<GamepadButton>());
    }

    private static float NormalizeTrigger(byte rawValue, float deadZone)
    {
        var normalized = (float)rawValue / byte.MaxValue;
        var magnitude = (normalized - deadZone) / (1f - deadZone);
        return normalized >= deadZone
            ? magnitude
            : 0f;
    }

    private static float NormalizeAxis(short rawValue, float deadZone)
    {
        var normalized = Mathf.Clamp((float)rawValue / short.MaxValue, -1f, 1f);
        var magnitude = Mathf.Abs(normalized) - deadZone;
        return magnitude > 0f
            ? normalized > 0f
                ? (normalized - deadZone) / (1f - deadZone)
                : (normalized + deadZone) / (1f - deadZone)
            : 0f;
    }
}