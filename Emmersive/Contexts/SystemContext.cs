using Emmersive.Helper;

namespace Emmersive.Contexts;

public class SystemContext : ContextProviderBase
{
    private const string ResourceKey = "Emmersive/SystemPrompt.txt";

    public override string Name => "system_prompt";

    public override object Build()
    {
        var resource = ResourceFetch.GetActiveResource(ResourceKey);
        if (!resource.IsEmpty()) {
            return resource;
        }

        resource = ResourceFetch.GetDefaultResource(ResourceKey);
        ResourceFetch.SetActiveResource(ResourceKey, resource);

        return resource;
    }
}