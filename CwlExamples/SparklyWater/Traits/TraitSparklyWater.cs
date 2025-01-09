using System.Collections.Generic;
using System.Linq;
using Cwl.LangMod;

namespace Sparkly.Traits;

internal class TraitSparklyWater : TraitDrink
{
    private static readonly HashSet<CardRow> _cached = [];

    internal static string SparklyName = "sparkly water";
    
    public override EffectId IdEffect => EffectId.Booze;
    public override bool IsNeg => false;

    public override void OnDrink(Chara c)
    {
        base.OnDrink(c);
        c.Say($"sparkly_love_{rnd(9)}", c);
    }

    internal static void TransformBooze(ref string traitName, Card traitOwner)
    {
        if (traitName != nameof(TraitDrink)) {
            return;
        }
        
        var row = traitOwner.sourceCard;
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
        row.name = "sparkly_fmt".Loc(row.name, SparklyName);
    }
}