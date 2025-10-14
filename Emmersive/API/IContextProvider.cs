namespace Emmersive.API;

public interface IContextProvider
{
    public bool IsAvailable { get; }
    public string Name { get; }
    public object? Build();
}