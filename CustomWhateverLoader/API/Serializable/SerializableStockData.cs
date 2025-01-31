using System;
using System.Collections.Generic;
using Cwl.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed record SerializableStockData : SerializableStockDataV2;

public sealed record SerializableStockItem : SerializableStockItemV3;

public record SerializableStockDataV2
{
    public List<SerializableStockItem> Items = [];
}

[Obsolete]
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
    public Thing Create(int lv = -1)
    {
        var thing = Type switch {
            StockItemType.Item => ThingGen.Create(Id, ReverseId.Material(Material), lv).SetNum(Num),
            StockItemType.Recipe => ThingGen.CreateRecipe(Id),
            StockItemType.Spell => ThingGen.CreateSpellbook(Id, Num),
            _ => ThingGen.Create(Id),
        };

        thing.ChangeRarity(Rarity);
        
        return thing;
    }
}

public record SerializableStockItemV2 : SerializableStockItemV1
{
    [JsonConverter(typeof(StringEnumConverter))]
    public Rarity Rarity = Rarity.Random;
}

public record SerializableStockItemV1
{
    public string Id = string.Empty;
    public string Material = string.Empty;
    public int Num = 1;
    public bool Restock = true;

    [JsonConverter(typeof(StringEnumConverter))]
    public StockItemType Type = StockItemType.Item;
}
// ReSharper restore All 