using System.Collections.Generic;
using System.Linq;
using Cwl.LangMod;
using Sparkly.Stats;

namespace Sparkly.Traits;

internal class TraitSparklyWater : TraitDrink
{
    private static readonly HashSet<CardRow> _cached = [];

    public override EffectId IdEffect => EffectId.DrinkWater;
    public override bool IsNeg => false;

    public override void OnDrink(Chara c)
    {
        base.OnDrink(c);

        c.AddCondition<ConCarbonated>(Power);
        c.Say($"sparkly_love_{rnd(9)}", c);
    }

    internal static void TransformBooze(ref string traitName, Card traitOwner)
    {
        if (traitName != nameof(TraitDrink) || traitOwner is not Thing thing) {
            return;
        }

        ref var row = ref thing.source;
        if (!row.trait.Contains(nameof(EffectId.Booze))) {
            return;
        }

        // mutate trait
        traitName = nameof(TraitSparklyWater);

        // mutate row
        if (!_cached.Add(row)) {
            return;
        }

        row.tag = row.tag
            .Where(t => t != nameof(CTAG.neg))
            .ToArray();
        row.name = "sparkly_fmt".Loc(row.name);
        row.name_L = "sparkly_fmt".Loc(row.name_L);
        row.name_JP = "sparkly_fmt".Loc(row.name_JP);
    }
}