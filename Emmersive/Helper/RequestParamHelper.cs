using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using Emmersive.LangMod;

namespace Emmersive.Helper;

public static class RequestParamHelper
{
    private static readonly HashSet<IChatProvider> _invalidParams = [];

    extension(IChatProvider provider)
    {
        public void SaveProviderParam()
        {
            if (_invalidParams.Contains(provider)) {
                return;
            }

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
            Dictionary<string, object>? requestParams;
            try {
                requestParams = provider.GetProviderParam();
            } catch (Exception ex) {
                _invalidParams.Add(provider);
                var name = provider is ChatProviderBase chat ? chat.Alias : provider.Id;
                var where = ex is JsonReaderException json ? $"{json.LineNumber}:{json.LinePosition}" : ex.Message;
                EmMod.WarnWithPopup<IChatProvider>($"[{name}] {"em_ui_err_params_invalid".Loc(where)}");
                return;
            }

            _invalidParams.Remove(provider);

            if (requestParams is null) {
                return;
            }

            provider.RequestParams.Clear();
            foreach (var (k, v) in requestParams) {
                provider.RequestParams[k] = v;
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