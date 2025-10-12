using System;

namespace Emmersive.Contexts;

public class ThingContext(Thing thing) : ContextProviderBase
{
    public override string Name => "thing_data";

    public override object? Build()
    {
        throw new NotImplementedException();
    }
}