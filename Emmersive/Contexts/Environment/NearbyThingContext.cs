using System;
using System.Collections.Generic;
using System.Linq;

namespace Emmersive.Contexts;

public class NearbyThingContext(Chara focus) : ContextProviderBase
{
    public override string Name => "nearby_things";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var dist = EmConfig.Context.NearbyRadius.Value;
        var center = focus.pos.Copy();

        var things = EClass._map.things
            .Where(t => t is { isHidden: false, isMasked: false, isRoofItem: false })
            .Where(t => center.Distance(t.pos) <= dist)
            .ToArray();

        var installed = Summarize(things
            .Where(t => t.IsInstalled)
            .Select(t => t.Name));
        var grounded = Summarize(things
            .Where(t => !t.IsInstalled)
            .Select(t => t.Name));

        var data = new Dictionary<string, object>(StringComparer.Ordinal);
        if (installed.Count > 0) {
            data["placed"] = installed;
        }

        if (grounded.Count > 0) {
            data["scattered"] = grounded;
        }

        return data.Count == 0 ? null : data;

        List<string> Summarize(IEnumerable<string> names)
        {
            return names
                .GroupBy(n => n)
                .Select(g => g.Count() > 1 ? $"{g.Key} x{g.Count()}" : g.Key)
                .ToList();
        }
    }
}