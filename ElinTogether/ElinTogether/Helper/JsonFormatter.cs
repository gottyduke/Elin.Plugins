using Cwl.Helper.FileUtil;
using Newtonsoft.Json;

namespace ElinTogether.Helper;

public static class JsonFormatter
{
    private static readonly JsonSerializerSettings _settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.Auto,
        ContractResolver = new ConfigCereal.WritablePropertiesOnlyResolver(),
    };

    extension(object context)
    {
        public string ToCompactJson()
        {
            return JsonConvert.SerializeObject(context, Formatting.None, _settings);
        }

        public string ToIndentedJson()
        {
            return JsonConvert.SerializeObject(context, Formatting.Indented, _settings);
        }
    }
}