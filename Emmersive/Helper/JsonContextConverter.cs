using Cwl.Helper.FileUtil;
using Newtonsoft.Json;

namespace Emmersive.Helper;

public static class JsonContextFormatter
{
    private static readonly JsonSerializerSettings _settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        TypeNameHandling = TypeNameHandling.None,
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