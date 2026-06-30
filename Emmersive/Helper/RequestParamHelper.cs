using System.Collections.Generic;
using System.IO;
using Emmersive.API;
using Emmersive.API.Services;

namespace Emmersive.Helper;

public static class RequestParamHelper
{
    extension(IChatProvider provider)
    {
        public void SaveProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"Params/{provider.Id}.txt");
            IO.SaveFile(path, provider.RequestParams, setting: JsonContextFormatter.Settings);
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

            var requestParams = IO.LoadFile<Dictionary<string, object>>(path, setting: JsonContextFormatter.Settings);
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

            Util.Run(path);
        }
    }
}