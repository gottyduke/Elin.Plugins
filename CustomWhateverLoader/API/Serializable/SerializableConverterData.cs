using System.Collections.Generic;

namespace Cwl.API;

public sealed record SerializableConverterData : SerializableConverterDataV1;

public sealed record SerializableConversionRule : SerializableConversionRuleV1;

public record SerializableConverterDataV1
{
    public Dictionary<string, SerializableConversionRule[]> Conversions = [];
    public int DecaySpeed = 500;
    public string IdMsg = "brew";
}

public record SerializableConversionRuleV1 : SerializableStockItemV3
{
    public string PriceAdd = "0";
}