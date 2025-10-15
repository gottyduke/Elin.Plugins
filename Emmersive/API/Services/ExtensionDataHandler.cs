using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cwl.Helper.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emmersive.API.Services;

public class ExtensionDataHandler()
    : DelegatingHandler(new HttpClientHandler {
        CheckCertificateRevocationList = true,
    })
{
    public static readonly ExtensionDataHandler Instance = new();

    protected override void Dispose(bool disposing)
    {
        // no dispose
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (ApiPoolSelector.Instance.CurrentProvider is not IExtensionMerger provider) {
            return await base.SendAsync(request, cancellationToken);
        }

        // merge into params
        var json = await request.Content.ReadAsStringAsync();
        try {
            var root = JObject.Parse(json);

            var dict = new Dictionary<string, object>();
            foreach (var prop in root.Properties()) {
                dict[prop.Name] = prop.Value.Type switch {
                    JTokenType.Object => prop.Value.ToObject<Dictionary<string, object>>()!,
                    JTokenType.Array => prop.Value.ToObject<object[]>()!,
                    JTokenType.Null => null!,
                    _ => ((JValue)prop.Value).Value!,
                };
            }

            provider.MergeExtensionData(dict);

            var merged = JsonConvert.SerializeObject(dict, Formatting.None);
            request.Content = new StringContent(merged, Encoding.UTF8, "application/json");
        } catch (Exception ex) {
            EmMod.Warn<ExtensionDataHandler>($"failed to merge ExtensionData into request\n{ex}");
            DebugThrow.Void(ex);
            // noexcept
        }

        request.Headers.Remove("Semantic-Kernel");
        request.Headers.Remove("User-Agent");

        return await base.SendAsync(request, cancellationToken);
    }
}