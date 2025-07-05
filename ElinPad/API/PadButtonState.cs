namespace ElinPad.API;

public record PadButtonState
{
    public float DownTime;
    public bool IsDown;
    public bool IsPressed;
    public float LastPressTime;
    public bool WasDown;
}