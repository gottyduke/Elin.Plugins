using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class NearbyCharaContext(Chara focus) : ContextProviderBase
{
    public override string Name => "nearby_characters";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var charas = focus.Nearby;
        if (charas.Count == 0) {
            return null;
        }

        Dictionary<string, object> data = new() {
            ["characters"] = charas
                .Select(c => new CharaContext(c).Build())
                .ToArray(),
        };

        var relationships = new RelationContext(charas).Build();
        if (relationships is not null) {
            data["relationships"] = relationships;
        }

        return data;
    }
}