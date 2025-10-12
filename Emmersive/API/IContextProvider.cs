namespace Emmersive.API;

public interface IContextProvider
{
    public bool IsDisabled { get; }
    public string Name { get; }
    public object? Build();
}