using System.Collections.Generic;
using Cwl.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed record SerializableStockData : SerializableStockDataV1;

public sealed record SerializableStockItem : SerializableStockItemV3;

public record SerializableStockDataV1
{
    public List<SerializableStockItem> Items = [];
    public string Owner = "";
}

public enum StockItemType
{
    Item,
    Recipe,
    Spell,
}

public record SerializableStockItemV3 : SerializableStockItemV2
{
    public bool Identified = true;

    public Thing Create(int lv = -1)
    {
        CardBlueprint.SetRarity(Rarity);

        var thing = Type switch {
            StockItemType.Item => ThingGen.Create(Id, ReverseId.Material(Material), lv).SetNum(Num),
            StockItemType.Recipe => ThingGen.CreateRecipe(Id),
            StockItemType.Spell => CreateSpellbook(),
            _ => ThingGen.Create(Id),
        };

        thing.c_IDTState = (int)(Identified ? IDTLevel.Identified : IDTLevel.RequireSuperiorIdentify);

        return thing;
    }

    private Thing CreateSpellbook()
    {
        var book = ThingGen.Create("spellbook").SetNum(Num);
        var elements = EMono.sources.elements;

        if (!elements.alias.TryGetValue(Id, out var row) &&
            !int.TryParse(Id, out var id) &&
            !elements.map.TryGetValue(id, out row)) {
            return book;
        }

        TraitSpellbook.Create(book, row.id);
        return book;
    }
}

public record SerializableStockItemV2 : SerializableStockItemV1
{
    [JsonConverter(typeof(StringEnumConverter))]
    public Rarity Rarity = Rarity.Normal;
}

public record SerializableStockItemV1
{
    public string Id = "";
    public string Material = "";
    public int Num = 1;
    public bool Restock = true;

    [JsonConverter(typeof(StringEnumConverter))]
    public StockItemType Type = StockItemType.Item;
}
// ReSharper restore All 