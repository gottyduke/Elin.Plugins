using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cwl.Helper.Exceptions;
using Emmersive.API.Exceptions;
using Emmersive.Components;
using Emmersive.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emmersive.API.Services;

public class ExtensionRequestHandler()
    : DelegatingHandler(new HttpClientHandler {
        CheckCertificateRevocationList = true,
    })
{
    public static readonly ExtensionRequestHandler Instance = new();

    protected override void Dispose(bool disposing)
    {
        // no dispose
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (ApiPoolSelector.Instance.CurrentProvider is not IExtensionRequestMerger provider) {
            EmMod.Debug<ExtensionRequestHandler>($"requesting {request.RequestUri}");
            return await base.SendAsync(request, cancellationToken);
        }

        // merge into params
        var json = await request.Content.ReadAsStringAsync();
        var finalized = "";
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

            provider.MergeExtensionRequest(dict, request);

            finalized = JsonConvert.SerializeObject(dict, Formatting.None);
            request.Content = new StringContent(finalized, Encoding.UTF8, "application/json");
        } catch (Exception ex) {
            EmMod.Warn<ExtensionRequestHandler>($"failed to merge ExtensionData into request\n{ex}");
            DebugThrow.Void(ex);
            // noexcept
        }

        ResetHeaders(request);

        ThrowIfDryRun(finalized);

        EmMod.Debug<ExtensionRequestHandler>($"requesting {request.RequestUri}");

        return await base.SendAsync(request, cancellationToken);
    }

    private static void ResetHeaders(HttpRequestMessage request)
    {
        request.Headers.Remove("Semantic-Kernel-Version");
        request.Headers.Remove("User-Agent");
        request.Headers.Remove("Accept");

        request.Headers.Add("Emmersive-Version", ModInfo.BuildVersion);
    }

    private static void ThrowIfDryRun(string requestBody)
    {
        if (EmScheduler.Mode != EmScheduler.ScheduleMode.DryRun) {
            return;
        }

        EmMod.Log<EmScheduler>(requestBody);

        ResourceFetch.SetCustomResource("dry_run.json", requestBody);
        ResourceFetch.OpenOrCreateCustomResource("dry_run.json");

        throw new SchedulerDryRunException();
    }
}