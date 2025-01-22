using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed record SerializableStockData : SerializableStockDataV2;

public sealed record SerializableStockItem : SerializableStockItemV2;

public record SerializableStockDataV2
{
    public List<SerializableStockItem> Items = [];
}

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