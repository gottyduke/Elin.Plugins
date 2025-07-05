using System;

namespace ElinPad.Native;

[Flags]
public enum GamepadButton
{
    None = 0,
    DPadUp = 1 << 0,
    DPadDown = 1 << 1,
    DPadLeft = 1 << 2,
    DPadRight = 1 << 3,
    Menu = 1 << 4,
    View = 1 << 5,
    LeftStick = 1 << 6,
    RightStick = 1 << 7,
    LeftBumper = 1 << 8,
    RightBumper = 1 << 9,
    A = 1 << 12,
    B = 1 << 13,
    X = 1 << 14,
    Y = 1 << 15,
}