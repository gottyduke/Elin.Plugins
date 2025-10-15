using System.Collections.Generic;

namespace Emmersive.Contexts;

public class ThingContext(Thing thing) : ContextProviderBase
{
    public override string Name => "thing_data";

    protected override IDictionary<string, object>? BuildInternal()
    {
        Dictionary<string, object> data = [];

        data["name"] = thing.Name;
        data["count"] = thing.Num;

        return data;
    }
}