using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ElinTogether.Helper;

public static class JsonFormatter
{
    public static readonly JsonSerializerSettings Settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        TypeNameHandling = TypeNameHandling.Auto,
        ContractResolver = new WritablePropertiesOnlyResolver(),
    };

    public class WritablePropertiesOnlyResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (member is PropertyInfo { CanWrite: false } ||
                member.IsDefined(typeof(NonSerializedAttribute), true)) {
                property.Ignored = true;
            }

            return property;
        }
    }

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