namespace Emmersive.API.Contexts;

public interface IContextProvider
{
    public bool IsAvailable { get; }
    public string Name { get; }
    public object? Build();
}