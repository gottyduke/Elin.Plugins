using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Emmersive.ChatProviders;
using Emmersive.Helper;
using Emmersive.LangMod;
using EModding.Helper;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;

namespace Emmersive.API.Services;

internal static class RequestFailure
{
    internal static string Describe(Exception ex, IChatProvider? provider = null)
    {
        ex = Unwrap(ex);

        switch (ex) {
            case HttpOperationException http:
                return http.StatusCode switch {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "em_ui_err_auth".lang(),
                    HttpStatusCode.NotFound => "em_ui_err_not_found".lang(),
                    (HttpStatusCode)429 => "em_ui_err_quota".lang(),
                    HttpStatusCode.BadRequest when http.ResponseContent?.Contains("response_format",
                            StringComparison.OrdinalIgnoreCase) is true
                        => "em_ui_err_response_format".lang(),
                    HttpStatusCode.BadRequest when ServerMessage(http.ResponseContent) is { } message
                        => "em_ui_err_bad_request".Loc(message),
                    null or 0 => Unreachable(provider),
                    { } code => "em_ui_err_generic".Loc((int)code),
                };
            case HttpRequestException or SocketException or WebException or TimeoutException or OperationCanceledException:
                return Unreachable(provider);
            default:
                EmMod.Warn($"[RequestFailure] {ex}");
                return "em_ui_err_unknown".lang();
        }
    }

    private static string Unreachable(IChatProvider? provider)
    {
        return provider switch {
            OllamaProvider => "em_ui_err_connect_local".Loc("Ollama"),
            Player2Provider => "em_ui_err_connect_local".Loc("Player2 App"),
            _ => "em_ui_err_connect".lang(),
        };
    }

    private static string? ServerMessage(string? content)
    {
        if (content.IsEmptyOrNull) {
            return null;
        }

        try {
            var message = JObject.Parse(content!)["error"] switch {
                JObject error => error["message"]?.ToString(),
                JValue value => value.ToString(CultureInfo.InvariantCulture),
                _ => null,
            };
            return message.IsWhiteSpaceOrNull ? null : message!.Trim().Truncate(100);
        } catch {
            return null;
        }
    }

    internal static bool IsRetryable(HttpOperationException http)
    {
        return http.StatusCode is not (HttpStatusCode.BadRequest or
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden or
            HttpStatusCode.NotFound);
    }

    internal static Exception Unwrap(Exception ex)
    {
        while (ex is AggregateException agg && agg.Flatten().InnerException is { } inner) {
            ex = inner;
        }

        return ex;
    }
}