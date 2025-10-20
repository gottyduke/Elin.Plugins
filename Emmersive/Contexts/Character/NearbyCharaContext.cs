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

        var charaContexts = new List<object>();
        var data = new Dictionary<string, object> {
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

        var religions = charas
            .Where(c => c is { hostility: >= Hostility.Friend, faith: not ReligionEyth })
            .Select(c => c.faith)
            .ToHashSet();
        if (religions.Count > 0) {
            var religionData = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var religion in religions) {
                var context = new ReligionContext(religion).Build();
                if (context is not null) {
                    religionData[religion.Name] = context;
                }
            }

            if (religions.Count > 0) {
                data["religions"] = religionData;
            }
        }

        return data;
    }
}