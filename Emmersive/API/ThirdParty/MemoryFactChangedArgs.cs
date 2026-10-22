namespace Emmersive.API.ThirdParty;

public enum MemoryFactChange
{
    Added,
    Updated,
    Forgotten,
}

public sealed class MemoryFactChangedArgs
{
    public required int Uid { get; init; }

    public required string Name { get; init; }

    public required MemoryFactChange Change { get; init; }

    public required string Fact { get; init; }

    public int Importance { get; init; }

    public string? OldFact { get; init; }

    public int OldImportance { get; init; }

    public Chara? Chara { get; init; }
}