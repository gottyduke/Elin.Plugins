using Emmersive.API.Services;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class SystemContext(string promptKey = "Emmersive/SystemPrompt.txt") : ContextProviderBase
{
    public override string Name => "system_prompt";

    public override object Build()
    {
        var resource = ResourceFetch.GetActiveResource(promptKey);
        if (!resource.IsEmptyOrNull) {
            return resource;
        }

        resource = ResourceFetch.GetDefaultResource(promptKey);
        ResourceFetch.SetActiveResource(promptKey, resource);

        return resource;
    }
}