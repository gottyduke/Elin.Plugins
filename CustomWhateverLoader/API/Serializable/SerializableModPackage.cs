namespace Cwl.API;

public sealed record SerializableModPackage : SerializableModPackageV1;

public record SerializableModPackageV1
{
    public string modId = "";
    public string modName = "";
}