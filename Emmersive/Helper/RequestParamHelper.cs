using System.Collections.Generic;
using System.IO;
using Cwl.Helper.FileUtil;
using Emmersive.API;
using Newtonsoft.Json;

namespace Emmersive.Helper;

public static class RequestParamHelper
{
    public static readonly JsonSerializerSettings Settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        TypeNameHandling = TypeNameHandling.None,
        ContractResolver = new ConfigCereal.WritablePropertiesOnlyResolver(),
    };

    extension(IChatProvider provider)
    {
        public void SaveProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"Params/{provider.Id}.txt");
            ConfigCereal.WriteConfig(provider.RequestParams, path, Settings);
        }

        public void RemoveProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"Params/{provider.Id}.txt");
            if (File.Exists(path)) {
                File.Delete(path);
            }

            path = Path.Combine(ResourceFetch.CustomFolder, $"Params/{provider.Id}.json");
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        public Dictionary<string, object>? GetProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"Params/{provider.Id}.txt");
            if (!File.Exists(path)) {
                path = Path.Combine(ResourceFetch.CustomFolder, $"Params/{provider.Id}.json");
            }

            ConfigCereal.ReadConfig<Dictionary<string, object>>(path, out var requestParams);
            return requestParams;
        }

        public void LoadProviderParam()
        {
            var requestParams = provider.GetProviderParam();
            if (requestParams is not null) {
                provider.RequestParams.Clear();
                foreach (var (k, v) in requestParams) {
                    provider.RequestParams[k] = v;
                }
            }
        }

        public void OpenProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"Params/{provider.Id}.txt");
            if (!File.Exists(path)) {
                provider.SaveProviderParam();
            }

            OpenFileOrPath.Run(path);
        }
    }
}