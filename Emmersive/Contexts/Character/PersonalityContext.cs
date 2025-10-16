using Emmersive.Helper;

namespace Emmersive.Contexts;

public class PersonalityContext(Chara chara) : ContextProviderBase
{
    public override string Name => "personality";

    public override object? Build()
    {
        var resourceKey = $"Emmersive/Persona/{chara.UnifiedId}.txt";
        var persona = ResourceFetch.GetActiveResource(resourceKey);
        return persona.IsEmpty() ? null : persona;
    }
}