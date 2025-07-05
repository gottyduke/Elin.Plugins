using ElinPad.API.Event;
using ElinPad.Native;

namespace ElinPad.Implementation.Event;

public class PadButtonEventArgs(PadButtonEventType type, GamepadButton button, float duration = 0f) :
    PadEventArgs
{
    public PadButtonEventType Type => type;
    public GamepadButton Button => button;
    public float Duration => duration;
}