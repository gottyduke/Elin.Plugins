using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed record SerializableStockData : SerializableStockDataV1;

public record SerializableStockDataV1
{
    public string Owner = "";
    public List<SerializableStockItem> Items = [];
}

public enum StockItemType
{
    Item,
    Recipe,
    Spell,
}

public record SerializableStockItem : SerializableStockItemV1;

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