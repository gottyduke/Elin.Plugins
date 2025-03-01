using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Runtime;
using Cwl.Helper.String;
using Cwl.LangMod;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.converter")]
public class CustomConverter : TraitBrewery
{
    private const string AltTraitName = $"Trait{nameof(CustomConverter)}";

    private static readonly Dictionary<string, Dictionary<string, SerializableConversionRule[]>> _cached = [];

    internal static readonly Dictionary<int, CustomConverter> Managed = [];
    internal static List<string>? PossibleTraits;

    public override int DecaySpeedChild => Data.DecaySpeed;
    public override Type type => default;
    public override string idMsg => Data.IdMsg;

    public SerializableConverterData Data { get; private set; } = new();
    public Dictionary<string, SerializableConversionRule[]> Conversions { get; private set; } = [];
    public HashSet<SerializableConversionRule> AllProducts { get; private set; } = [];

    public override bool CanChildDecay(Card card)
    {
        return Conversions.ContainsKey(card.id) || AllProducts.All(p => p.Id != card.id);
    }

    [SwallowExceptions]
    public override bool OnChildDecay(Card card, bool firstDecay)
    {
        if (!firstDecay) {
            return true;
        }

        foreach (var product in GenerateProducts(owner, card)) {
            _OnProduce((Thing)card, product);
            if (owner.trait != this) {
                owner.trait.InstanceDispatch("_OnProduce", card, product);
            }
        }

        return false;
    }

    private void _OnProduce(Thing ingredient, Thing product)
    {
        pc.Say(idMsg, ingredient, product);
        owner.AddThing(product);
    }

    public static Thing[] GenerateProducts(Card owner, Card card)
    {
        var converter = GetConverter(owner);
        if (converter is null) {
            return [];
        }

        var conversions = converter.Conversions;
        if (!conversions.TryGetValue(card.id, out var products) || products is null) {
            products = conversions.GetValueOrDefault(card.sourceCard._origin, []);
        }

        List<Thing> generated = [];
        foreach (var product in products) {
            var thing = product.Create(card.LV).SetNum(Mathf.Max(1, product.Num * card.Num));

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

            generated.Add(thing);
        }

        return generated.ToArray();
    }

    [ConsoleCommand("reload")]
    [CwlContextMenu("Converter/Reload", "cwl_ui_converter_reload")]
    public static string ReloadAllConverterData()
    {
        _cached.Clear();

        foreach (var card in _map.Cards) {
            if (!Managed.ContainsKey(card.uid)) {
                continue;
            }

            if (!LoadConverterData(card)) {
                Managed.Remove(card.uid);
            }
        }

        return "reloaded";
    }

    public static CustomConverter? GetConverter(Card card)
    {
        return Managed.GetValueOrDefault(card.uid);
    }

    public static bool LoadConverterData(Card card)
    {
        var converter = new CustomConverter();

        var dataId = card.sourceCard.trait.TryGet(5, true) ?? card.id;
        if (!_cached.TryGetValue(dataId, out var data)) {
            var (_, serialized) = PackageIterator.GetJsonsFromPackage<SerializableConverterData>($"Data/converter_{dataId}.json")
                .LastOrDefault();
            if (serialized is null) {
                return false;
            }

            data = _cached[dataId] = new();
            foreach (var (id, products) in serialized.Conversions) {
                if (id.StartsWith("origin:")) {
                    foreach (var idv in sources.things.map.Values.Where(r => r._origin == id[7..])) {
                        data[idv.id] = products;
                    }
                } else {
                    data[id] = products;
                }
            }

            CwlMod.Log<CustomConverter>("cwl_log_converter_apply".Loc(dataId, card.id));
            converter.Data = serialized;
        }

        converter.Conversions = data;
        converter.AllProducts.Clear();
        converter.AllProducts.UnionWith(data.Values.SelectMany(p => p));

        Managed[card.uid] = converter;
        return true;
    }

    internal static void TransformConverter(ref string traitName, Card traitOwner)
    {
        PossibleTraits ??= TypeQualifier.Declared
            .OfDerived(typeof(TraitBrewery))
            .Select(t => t.Name)
            .Concat([AltTraitName])
            .ToList();

        if (PossibleTraits.Contains(traitName)) {
            LoadConverterData(traitOwner);
        }

        if (traitName == AltTraitName) {
            traitName = nameof(CustomConverter);
        }
    }
}