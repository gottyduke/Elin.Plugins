using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API.Exceptions;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using Emmersive.Components;
using Emmersive.Helper;
using Emmersive.LangMod;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;

namespace Emmersive.API.ThirdParty;

public static class EmAi
{
    /// <summary>At least one service is configured and not on cooldown.</summary>
    public static bool IsAvailable => ApiPoolSelector.Instance.HasAnyAvailableServices();

    /// <summary>Ids of every registered service, usable as providerId.</summary>
    public static IReadOnlyList<string> GetModels()
    {
        return [
            ..ApiPoolSelector.Instance.Providers
                .Select(p => p.Id),
        ];
    }

    /// <param name="providerId">a specific service id, or null for the next available one</param>
    /// <param name="rawOutput">true for whatever the model says, false to use EM scene schema</param>
    public static UniTask<RequestReport> SendWithReportAsync(
        string systemPrompt,
        string userMessage,
        string? providerId = null,
        CancellationToken ct = default,
        bool rawOutput = true)
    {
        return SendAsync(systemPrompt, userMessage, rawOutput, null, providerId, ct);
    }

    public static UniTask<RequestReport> SendWithReportAsync(
        string systemPrompt,
        string userMessage,
        string? providerId = null,
        JObject? responseSchema = null,
        CancellationToken ct = default)
    {
        return SendAsync(systemPrompt, userMessage, true, responseSchema, providerId, ct);
    }

    private static async UniTask<RequestReport> SendAsync(string systemPrompt,
                                                          string userMessage,
                                                          bool rawOutput,
                                                          JObject? responseSchema,
                                                          string? providerId,
                                                          CancellationToken ct)
    {
        var apiPool = ApiPoolSelector.Instance;
        IChatProvider? provider;

        if (providerId is not null) {
            provider = apiPool.Providers.FirstOrDefault(p => p.Id == providerId);
            if (provider is null) {
                return RequestReport.Fail(RequestStatus.NoProvider, $"Provider '{providerId}' not found.");
            }

            provider.UpdateAvailability();
            if (!provider.IsAvailable) {
                return RequestReport.Fail(RequestStatus.Unavailable, $"Provider '{providerId}' is currently unavailable.",
                    providerId);
            }
        } else if (!apiPool.TryGetNextAvailable(out provider)) {
            return apiPool.Providers.Count == 0
                ? RequestReport.Fail(RequestStatus.NoProvider,
                    "No AI providers registered. Add a service in the Emmersive panel.")
                : RequestReport.Fail(RequestStatus.Unavailable,
                    "All registered providers are currently unavailable (cooldown or misconfigured).");
        }

        var kernel = EmKernel.Kernel ?? EmKernel.RebuildKernel();

        ChatHistory history = [];
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userMessage);

        using var activity = EmActivity.StartNew(provider.Id);

        try {
            var response = provider is ChatProviderBase builtin
                ? await builtin.HandleRequestWithSchema(kernel, history, activity, rawOutput, responseSchema, ct)
                : await provider.HandleRequest(kernel, history, activity, rawOutput, ct);
            await UniTask.SwitchToMainThread();

            if (response.Content.IsEmptyOrNull) {
                activity.SetStatus(EmActivity.StatusType.Failed);
                return RequestReport.Fail(RequestStatus.EmptyResponse, "Provider returned an empty response.", provider.Id);
            }

            activity.SetStatus(EmActivity.StatusType.Completed);
            return RequestReport.Ok(response.Content!, provider.Id, activity);
        } catch (Exception ex) {
            await UniTask.SwitchToMainThread();
            return OnFailure(RequestFailure.Unwrap(ex));
        }

        RequestReport OnFailure(Exception ex)
        {
            switch (ex) {
                case SchedulerDryRunException:
                    activity.SetStatus(EmActivity.StatusType.Unknown);
                    EmScheduler.RestoreFromDryRun();
                    return RequestReport.Fail(RequestStatus.DryRun, "Dry run, request was not sent.", provider.Id);
                case OperationCanceledException when ct.IsCancellationRequested:
                    activity.SetStatus(EmActivity.StatusType.Failed);
                    return RequestReport.Fail(RequestStatus.Cancelled, "Request was cancelled.", provider.Id);
                case OperationCanceledException: {
                    activity.SetStatus(EmActivity.StatusType.Timeout);
                    var timeout = provider is ChatProviderBase chat
                        ? chat.TimeoutSeconds
                        : EmConfig.Policy.Timeout.Value;
                    var reason = "em_ui_err_timeout".Loc(timeout);
                    provider.MarkUnavailable(reason);
                    return RequestReport.Fail(RequestStatus.Timeout, "Request timed out.", provider.Id, reason);
                }
                case HttpOperationException { StatusCode: HttpStatusCode.BadRequest }:
                    activity.SetStatus(EmActivity.StatusType.Failed);
                    return Fail(RequestStatus.HttpError, RequestFailure.Describe(ex, provider));
                default: {
                    activity.SetStatus(EmActivity.StatusType.Failed);
                    var reason = RequestFailure.Describe(ex, provider);
                    provider.MarkUnavailable(reason);

                    return Fail(ex is HttpOperationException ? RequestStatus.HttpError : RequestStatus.Failed, reason);
                }
            }
        }

        RequestReport Fail(RequestStatus status, string reason) =>
            RequestReport.Fail(status, $"Request failed: {reason}", provider.Id, reason);
    }
}