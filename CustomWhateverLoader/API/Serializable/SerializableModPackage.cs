namespace Cwl.API;

public sealed record SerializableModPackage : SerializableModPackageV2;

public record SerializableModPackageV2 : SerializableModPackageV1
{
    public virtual bool Equals(SerializableModPackageV2? rhs)
    {
        return ModId == rhs?.ModId;
    }

    public override int GetHashCode()
    {
        return ModId.GetHashCode();
    }
}

public record SerializableModPackageV1
{
    public string ModId { get; init; } = "";
    public string ModName { get; init; } = "";
}