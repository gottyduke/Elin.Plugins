using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
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
            var path = Path.Combine(ResourceFetch.CustomFolder, $"params/{provider.Id}.json");
            ConfigCereal.WriteConfig(provider.RequestParams, path, Settings);
        }

        public void RemoveProviderParam()
        {
            var path = Path.Combine(ResourceFetch.CustomFolder, $"params/{provider.Id}.json");
            if (File.Exists(path)) {
                File.Delete(path);
            }
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
            if (!File.Exists(path)) {
                provider.SaveProviderParam();
            }

            try {
                Util.Run(path);
            } catch {
                EmMod.Popup<ResourceFetch>("em_ui_failed_shellex".Loc());

                try {
                    Process.Start("notepad.exe", path);
                } catch (Exception ex) {
                    EmMod.Popup<ResourceFetch>("em_ui_failed_shellex".Loc(path, ex.Message));
                    Util.Run(Path.GetDirectoryName(path));
                    // noexcept
                }
                // noexcept
            }
        }
    }
}