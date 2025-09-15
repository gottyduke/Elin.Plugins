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
        CardBlueprint bp = CardBlueprint.current ?? CardBlueprint._Default;
        bp.rarity = Rarity;

        var thing = Type switch {
            StockItemType.Item => ThingGen.Create(Id, ReverseId.Material(Material), lv).SetNum(Num),
            StockItemType.Recipe => ThingGen.CreateRecipe(Id),
            StockItemType.Spell => EMono.sources.elements.alias.TryGetValue(Id, out var row)
                ? ThingGen.CreateSpellbook(row.id, 1, Num)
                : int.TryParse(Id, out var ele)
                    ? ThingGen.CreateSpellbook(ele, 1, Num)
                    : ThingGen.Create(Id),
            _ => ThingGen.Create(Id),
        };

        thing.c_IDTState = Identified ? 0 : 1;

        return thing;
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