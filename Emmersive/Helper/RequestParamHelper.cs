using System.Collections.Generic;
using System.IO;
using Cwl.Helper.FileUtil;
using Emmersive.API;

namespace Emmersive.Helper;

public static class RequestParamHelper
{
    extension(IChatProvider provider)
    {
        public void SaveProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"params/{provider.Id}.json");
            ConfigCereal.WriteConfig(provider.RequestParams, path);
        }

        public void RemoveProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"params/{provider.Id}.json");
            File.Delete(path);
        }

        public Dictionary<string, object>? GetProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"params/{provider.Id}.json");
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
            var path = Path.Combine(ResourceFetch.CustomFolder, $"params/{provider.Id}.json");
            Util.Run(path);
        }
    }
}