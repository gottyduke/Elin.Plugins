using System.Linq;

namespace Emmersive.Contexts;

public class NearbyThingContext(Chara focus) : ContextProviderBase
{
    public override string Name => "nearby_things";

    public override object? Build()
    {
        var things = focus.pos.detail.things
            .Where(t => t is { isHidden: false, isMasked: false, isRoofItem: false })
            .ToArray();

        if (things.Length == 0) {
            return null;
        }

        return things
            .Select(t => new ThingContext(t).Build())
            .ToArray();
    }
}