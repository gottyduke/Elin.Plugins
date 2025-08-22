using System;
using Newtonsoft.Json;

namespace Cwl.Helper.FileUtil;

public class RangedIntConverter(int min, int max) : JsonConverter<int>
{
    public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return hasExistingValue
            ? Math.Clamp(existingValue, min, max)
            : min;
    }

    public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}

public class RangedFloatConverter(float min, float max) : JsonConverter<float>
{
    public override float ReadJson(JsonReader reader, Type objectType, float existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return hasExistingValue
            ? Math.Clamp(existingValue, min, max)
            : min;
    }

    public override void WriteJson(JsonWriter writer, float value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}