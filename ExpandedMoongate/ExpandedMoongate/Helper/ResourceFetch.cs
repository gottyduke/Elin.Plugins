namespace Exm.Helper;

public class ResourceFetch
{
    public static readonly GameIOContext Context = GameIOContext.GetPersistentModContext("ExpandedMoongate")!;

    public static string CustomFolder => Context.GetFullPath("Custom");
}