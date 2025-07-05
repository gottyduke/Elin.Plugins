using ElinPad.Implementation.Event;
using ElinPad.Native;

namespace ElinPad.API.Event;

public interface IPadEventHandler
{
    public virtual void AcceptButton(Gamepad input, PadButtonEventArgs args)
    {
    }

    public virtual void AcceptAxis(Gamepad input, PadAxisEventArgs args)
    {
    }
}