using Windows.Win32;

namespace ElinPad.Native;

public class Gamepad(GamepadIndex index)
{
    public GamepadIndex Index => index;
    public bool IsConnected => TryGetState(out _);

    public bool TryGetState(out GamepadState state)
    {
        var success = PInvoke.XInputGetState((uint)index, out var pState) == 0;
        state = success
            ? new(pState.Gamepad, pState.dwPacketNumber)
            : new();
        return success;
    }
}