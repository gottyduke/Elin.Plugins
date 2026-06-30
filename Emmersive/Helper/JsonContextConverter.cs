using Newtonsoft.Json;

namespace Emmersive.Helper;

public static class JsonContextFormatter
{
    public static readonly JsonSerializerSettings Settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        TypeNameHandling = TypeNameHandling.None,
        ContractResolver = new GameIOContext.WritablePropertiesOnlyResolver(),
    };

    extension(object context)
    {
        public string ToCompactJson()
        {
            return JsonConvert.SerializeObject(context, Formatting.None, Settings);
        }

        public string ToIndentedJson()
        {
            return JsonConvert.SerializeObject(context, Formatting.Indented, Settings);
        }
    }
}