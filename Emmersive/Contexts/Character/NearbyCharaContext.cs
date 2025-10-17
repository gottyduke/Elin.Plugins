using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Exceptions;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class NearbyCharaContext(Chara focus) : ContextProviderBase
{
    public override string Name => "nearby_characters";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var charas = focus.Nearby.Copy();
        if (charas.Count == 0) {
            return null;
        }

        List<object> charaContexts = [];
        Dictionary<string, object> data = new() {
            ["characters"] = charaContexts,
        };

        foreach (var chara in charas.Select(c => new CharaContext(c))) {
            try {
                charaContexts.Add(chara.Build());
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                // noexcept
            }
        }

        var relationships = new RelationContext(charas).Build();
        if (relationships is not null) {
            data["relationships"] = relationships;
        }

        return data;
    }
}