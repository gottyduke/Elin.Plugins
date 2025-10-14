using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cwl.Helper.Exceptions;

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
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Dictionary<string, object> dict = [];
            foreach (var prop in root.EnumerateObject()) {
                dict[prop.Name] = prop.Value.ValueKind switch {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(prop.Value.GetRawText())!,
                    _ => JsonSerializer.Deserialize<object>(prop.Value.GetRawText())!,
                };
            }

            provider.MergeExtensionData(dict);

            var merged = JsonSerializer.Serialize(dict);
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(merged));
            request.Content.Headers.ContentType = new("application/json") {
                CharSet = "utf-8",
            };
        } catch (Exception ex) {
            EmMod.Warn<ExtensionDataHandler>($"failed to merge ExtensionData into request\n{ex}");
            DebugThrow.Void(ex);
            // noexcept
        }

        return await base.SendAsync(request, cancellationToken);
    }
}