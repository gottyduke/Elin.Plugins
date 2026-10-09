using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Emmersive.API.Exceptions;
using Emmersive.ChatProviders;
using Emmersive.Components;
using Emmersive.Helper;
using Emmersive.LangMod;
using EModding.Helper.Runtime.Exceptions;
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
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (provider, _, _) = ChatProviderBase.CurrentScope;
        var body = await ReadBodyAsync(request).ConfigureAwait(false);

        if (provider is IExtensionRequestMerger merger && body is not null) {
            try {
                merger.MergeExtensionRequest(body, request);

                var finalized = JsonConvert.SerializeObject(body, Formatting.None);
                request.Content = new StringContent(finalized, Encoding.UTF8, "application/json");
            } catch (Exception ex) {
                EmMod.Warn<ExtensionRequestHandler>($"failed to merge ExtensionData into request\n{ex}");
                DebugThrow.Void(ex);
                // noexcept
            }
        }

        ResetHeaders(request);

        if (EmScheduler.Mode == EmScheduler.SchedulerMode.DryRun) {
            DumpDryRun(request, body);
            throw new SchedulerDryRunException();
        }

        EmMod.Debug<ExtensionRequestHandler>($"requesting {request.RequestUri}");

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, object>?> ReadBodyAsync(HttpRequestMessage request)
    {
        if (request.Content is null) {
            return null;
        }

        try {
            var json = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            var root = JObject.Parse(json);

            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in root.Properties()) {
                dict[prop.Name] = prop.Value.Type switch {
                    JTokenType.Object => prop.Value.ToObject<Dictionary<string, object>>()!,
                    JTokenType.Array => prop.Value.ToObject<object[]>()!,
                    JTokenType.Null => null!,
                    _ => ((JValue)prop.Value).Value!,
                };
            }

            return dict;
        } catch (Exception ex) {
            EmMod.Warn<ExtensionRequestHandler>($"failed to read request body\n{ex}");
            return null;
            // noexcept
        }
    }

    private static void DumpDryRun(HttpRequestMessage request, Dictionary<string, object>? body)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"[{request.Method}]: {request.RequestUri}");
        sb.Append("[Content]: ").Append(body?.ToIndentedJson() ?? "");

        var log = sb.ToString();
        EmMod.Log<EmScheduler>(log);

        const string file = "dry_run.txt";
        ResourceFetch.SetCustomResource(file, log);
        ResourceFetch.OpenOrCreateCustomResource(file);

        var path = Path.GetFullPath(ResourceFetch.CustomFolder + file);
        UniTask.Post(() => EmMod.Popup<EmScheduler>("em_ui_dry_run_done".Loc(path), 10f));
    }

    private static void ResetHeaders(HttpRequestMessage request)
    {
        request.Headers.Remove("Semantic-Kernel-Version");
        request.Headers.Remove("User-Agent");
        request.Headers.Remove("Accept");

        request.Headers.Add("Emmersive-Version", ModInfo.BuildVersion);
    }
}