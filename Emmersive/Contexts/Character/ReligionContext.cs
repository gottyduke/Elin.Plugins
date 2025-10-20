namespace Emmersive.Contexts;

internal class ReligionContext(Religion religion) : ContextProviderBase
{
    public override string Name => "religion_data";

    public override object? Build()
    {
        return religion.source.GetDetail();
    }
}