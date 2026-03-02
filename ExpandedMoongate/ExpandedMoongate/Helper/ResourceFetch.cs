using Cwl.API.Processors;

namespace EGate.Helper;

public class ResourceFetch
{
    // holds active data exchanges
    public static readonly GameIOProcessor.GameIOContext Context = GameIOProcessor.GetPersistentModContext("ExpandedMoongate")!;

    public static string CustomFolder => Context.GetPath("Custom");
}