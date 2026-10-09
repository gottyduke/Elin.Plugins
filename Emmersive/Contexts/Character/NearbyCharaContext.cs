using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;
using EModding.Helper.Runtime.Exceptions;

namespace Emmersive.Contexts;

public class NearbyCharaContext(Chara focus) : ContextProviderBase
{
    public override string Name => "nearby_characters";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var charas = GetNearbyChara(focus);
        if (charas.Count == 0) {
            return null;
        }

        var charaContexts = new Dictionary<string, object>(StringComparer.Ordinal);
        var data = new Dictionary<string, object> {
            ["characters"] = charaContexts,
        };

        foreach (var chara in charas) {
            try {
                var charaContext = new CharaContext(chara);
                if (!charaContext.IsAvailable || charaContext.Build() is not { } context) {
                    continue;
                }

                var key = charaContexts.ContainsKey(chara.NameSimple)
                    ? $"{chara.NameSimple}#{chara.uid}"
                    : chara.NameSimple;

                charaContexts[key] = context;
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                // noexcept
            }
        }

        var relation = new RelationContext([..charas, EClass.pc]);
        if (relation.IsAvailable && relation.Build() is { } relationships) {
            data["relationships"] = relationships;
        }

        return data;
    }

    public static List<Chara> GetNearbyChara(Chara focus)
    {
        var charas = focus.Nearby
            .Distinct(UniqueCardComparer.Default)
            .OfType<Chara>()
            .Where(c => c.Profile.CanTrigger)
            .OrderByDescending(CharaSorter)
            .Take(EmConfig.Context.NearbyMaxCount.Value)
            .ToList();

        return charas;

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