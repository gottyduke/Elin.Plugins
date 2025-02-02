using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using ReflexCLI.Attributes;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.converter")]
public class CustomConverter : TraitBrewery
{
    private static readonly Dictionary<string, Dictionary<string, SerializableConversionRule[]>> _cached = [];

    public override int DecaySpeedChild => Data.DecaySpeed;
    public override Type type => default;
    public override string idMsg => Data.IdMsg;

    public SerializableConverterData Data { get; private set; } = new();
    public Dictionary<string, SerializableConversionRule[]> Conversions { get; private set; } = [];
    public HashSet<SerializableConversionRule> AllProducts { get; private set; } = [];

    public override bool CanChildDecay(Card card)
    {
        return Conversions.ContainsKey(card.id) || AllProducts.FirstOrDefault(p => p.Id == card.id) is null;
    }

    [SwallowExceptions]
    public override bool OnChildDecay(Card card, bool firstDecay)
    {
        if (!firstDecay) {
            return true;
        }

        if (!Conversions.TryGetValue(card.id, out var products) || products is null) {
            products = Conversions.GetValueOrDefault(card.sourceCard._origin, []);
        }

        if (products.Length == 0) {
            return true;
        }

        foreach (var product in products) {
            var thing = product.Create(card.LV);

            var food = thing.IsFood;
            CraftUtil.MixIngredients(thing, [card.Thing], food ? CraftUtil.MixType.Food : CraftUtil.MixType.General, 999, pc);
            if (food) {
                thing.MakeFoodRef(card);
            } else {
                thing.MakeRefFrom(card);
            }

            if (product.PriceAdd.TryEvaluate<int>(out var added, new { @base = card.GetValue() })) {
                thing.c_priceAdd = added;
            }

            if (!card.isDestroyed) {
                card.Destroy();
            }

            owner.AddThing(thing);
            owner.GetRootCard().Say(idMsg, card, thing);
        }

        return false;
    }

    public override void OnSetOwner()
    {
        var dataId = GetParam(5, owner.id);
        if (!_cached.TryGetValue(dataId, out var data)) {
            var conv = PackageIterator.GetRelocatedJsonsFromPackage<SerializableConverterData>($"Data/converter_{dataId}.json")
                .LastOrDefault();
            if (conv.Item2 is null) {
                CwlMod.WarnWithPopup<CustomConverter>("cwl_warn_converter_missing".Loc(dataId, owner.id));
                return;
            }

            Data = conv.Item2;
            data = new();

            foreach (var (id, products) in Data.Conversions) {
                if (id.StartsWith("origin:")) {
                    foreach (var idv in sources.things.map.Values.Where(r => r._origin == id[7..])) {
                        data[idv.id] = products;
                    }
                } else {
                    data[id] = products;
                }
            }

            _cached[dataId] = data;
            CwlMod.Log<CustomConverter>("cwl_log_converter_apply".Loc(dataId, owner.id));
        }

        Conversions = data;
        AllProducts.Clear();
        AllProducts.UnionWith(data.Values.SelectMany(p => p));
    }

    [ConsoleCommand("reload")]
    public static void ReloadAllConverterData()
    {
        _cached.Clear();
        foreach (var card in _map.Cards) {
            if (card.trait is CustomConverter converter) {
                converter.OnSetOwner();
            }
        }
    }

    internal static void TransformConverter(ref string traitName, Card traitOwner)
    {
        if (traitName == $"Trait{nameof(CustomConverter)}") {
            traitName = nameof(CustomConverter);
        }
    }
}