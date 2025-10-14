using Emmersive.Helper;

namespace Emmersive.Contexts;

public class SystemContext : ContextProviderBase
{
    private readonly string _prompt = ResourceFetch.GetActiveResource("Emmersive/SystemPrompt.txt");

    public override string Name => "system_prompt";

    public override object Build()
    {
        return _prompt;
    }
}