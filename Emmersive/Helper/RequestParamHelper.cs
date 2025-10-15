using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
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

            // TODO: use CWL version after updating
            //OpenFileOrPath.Run(path);

            path = path.NormalizePath();
            try {
                Process.Start(path);
            } catch {
                EmMod.Popup<OpenFileOrPath>("em_ui_failed_shellex".Loc());

                var proton = !"PROTON_VERSION".EnvVar.IsEmpty() ||
                             !"STEAM_COMPAT_DATA_PATH".EnvVar.IsEmpty();

                try {
                    if (proton) {
                        Process.Start("xdg-open", $"\"{path}\"");
                    } else {
                        Process.Start("notepad.exe", path);
                    }
                } catch {
                    try {
                        if (proton) {
                            Process.Start("xdg-open", $"\"{path}\"");
                        } else {
                            Process.Start("notepad.exe", path);
                        }
                    } catch {
                        // noexcept
                    }
                    // noexcept
                }
                // noexcept
            }
        }
    }
}