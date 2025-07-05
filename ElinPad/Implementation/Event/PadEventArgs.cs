using System;
using ElinPad.Native;

namespace ElinPad.Implementation.Event;

public class PadEventArgs : EventArgs
{
    public GamepadState Gamepad { get; set; }
}