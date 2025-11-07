using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class NearbyCharaContext(Chara focus) : ContextProviderBase
{
    public override string Name => "nearby_characters";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var charas = focus.Nearby
            .Distinct(UniqueCardComparer.Default)
            .OfType<Chara>()
            .Where(c => c.Profile.CanTrigger)
            .OrderByDescending(CharaSorter)
            .TakeLast(EmConfig.Context.NearbyMaxCount.Value)
            .ToList();
        if (charas.Count == 0) {
            return null;
        }

        var charaContexts = new Dictionary<string, object>(StringComparer.Ordinal);
        var data = new Dictionary<string, object> {
            ["characters"] = charaContexts,
        };

        foreach (var chara in charas) {
            try {
                var context = new CharaContext(chara).Build();
                if (context is not null) {
                    charaContexts[chara.NameSimple] = context;
                }
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                // noexcept
            }
        }

        var relationships = new RelationContext(charas).Build();
        if (relationships is not null) {
            data["relationships"] = relationships;
        }

        /* TODO disable religion for now

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
         */

        return data;

        int CharaSorter(Chara owner)
        {
            var priority = 0;

            if (owner.IsPCParty) {
                priority += 3;
            }

            if (owner.IsPCFaction) {
                priority += 2;
            }

            if (owner.IsUnique) {
                priority++;
            }

            if (owner.IsGlobal) {
                priority++;
            }

            return priority;
        }
    }
}