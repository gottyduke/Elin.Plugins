using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
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
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        try {
            var root = JObject.Parse(json);

            foreach (var prop in root.Properties()) {
                dict[prop.Name] = prop.Value.Type switch {
                    JTokenType.Object => prop.Value.ToObject<Dictionary<string, object>>()!,
                    JTokenType.Array => prop.Value.ToObject<object[]>()!,
                    JTokenType.Null => null!,
                    _ => ((JValue)prop.Value).Value!,
                };
            }

            provider.MergeExtensionRequest(dict, request);

            var finalized = JsonConvert.SerializeObject(dict, Formatting.None);
            request.Content = new StringContent(finalized, Encoding.UTF8, "application/json");
        } catch (Exception ex) {
            EmMod.Warn<ExtensionRequestHandler>($"failed to merge ExtensionData into request\n{ex}");
            DebugThrow.Void(ex);
            // noexcept
        }

        ResetHeaders(request);

        ThrowIfDryRun();

        EmMod.Debug<ExtensionRequestHandler>($"requesting {request.RequestUri}");

        return await base.SendAsync(request, cancellationToken);

        void ThrowIfDryRun()
        {
            if (EmScheduler.Mode != EmScheduler.ScheduleMode.DryRun) {
                return;
            }

            using var sb = StringBuilderPool.Get();

            sb.AppendLine($"[{request.Method}]: {request.RequestUri}");
            sb.Append("[Content]: ").Append(dict.ToIndentedJson());

            var log = sb.ToString();
            EmMod.Log<EmScheduler>(log);

            ResourceFetch.SetCustomResource("dry_run.txt", log);
            ResourceFetch.OpenOrCreateCustomResource("dry_run.txt");

            EmScheduler.SwitchMode(EmScheduler.ScheduleMode.Buffer);

            throw new SchedulerDryRunException();
        }
    }

    private static void ResetHeaders(HttpRequestMessage request)
    {
        request.Headers.Remove("Semantic-Kernel-Version");
        request.Headers.Remove("User-Agent");
        request.Headers.Remove("Accept");

        request.Headers.Add("Emmersive-Version", ModInfo.BuildVersion);
    }
}